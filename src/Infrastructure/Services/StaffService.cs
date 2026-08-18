using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Staff;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIssueService _issueService;
    private readonly IIssueAssignmentMemberService _issueAssignmentMemberService;
    private readonly IIssueAssignmentService _issueAssignmentService;
    private readonly IDepartmentMemberService _departmentMemberService;

    public StaffService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IIssueService issueService,
        IIssueAssignmentMemberService issueAssignmentMemberService,
        IIssueAssignmentService issueAssignmentService,
        IDepartmentMemberService departmentMemberService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _issueService = issueService;
        _issueAssignmentMemberService = issueAssignmentMemberService;
        _issueAssignmentService = issueAssignmentService;
        _departmentMemberService = departmentMemberService;
    }

    public async Task<StaffDashboardSummaryResponse?> GetDashboardSummaryAsync(string staffId, CancellationToken cancellationToken = default)
    {
        var staffDepartment = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(dm => dm.UserId == staffId && dm.IsActive, cancellationToken);
        if (staffDepartment == null) return null;

        var departmentId = staffDepartment.DepartmentId;
        var now = DateTime.UtcNow;

        var newIncidentsInDepartment = await _context.Issues
            .Where(i => i.ReportedAt >= now.AddHours(-24) &&
                        i.Assignments.Any(ia => ia.DepartmentId == departmentId && ia.IsCurrent))
            .CountAsync(cancellationToken);

        var myInProgressTasks = await _context.IssueAssignmentMembers
            .Where(iam => iam.UserId == staffId && iam.Status == AssignmentMemberStatus.Accepted)
            .CountAsync(cancellationToken);

        var myPendingAssignments = await _context.IssueAssignmentMembers
            .Where(iam => iam.UserId == staffId && iam.Status == AssignmentMemberStatus.Pending)
            .CountAsync(cancellationToken);

        return new StaffDashboardSummaryResponse
        {
            NewIncidentsInDepartment = newIncidentsInDepartment,
            MyInProgressTasks = myInProgressTasks,
            MyPendingAssignments = myPendingAssignments
        };
    }

    public async Task<PagedResponse<StaffTaskResponse>> GetMyTasksAsync(GetMyTasksRequest request, string staffId, CancellationToken cancellationToken = default)
    {
        var query = _context.IssueAssignmentMembers
            .AsNoTracking()
            .Where(iam => iam.UserId == staffId && iam.Status == AssignmentMemberStatus.Accepted)
            .Include(iam => iam.Assignment)
                .ThenInclude(a => a.Issue)
                    .ThenInclude(i => i.Status)
            .Include(iam => iam.Assignment)
                .ThenInclude(a => a.Issue)
                    .ThenInclude(i => i.Priority)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(iam => iam.Assignment.Issue.Status.StatusCode == request.Status.ToUpperInvariant());
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(iam => iam.Assignment.Issue.Priority.PriorityId == request.Priority.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(iam => iam.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(iam => new StaffTaskResponse
            {
                IssueId = iam.Assignment.Issue.IssueId,
                Title = iam.Assignment.Issue.Title,
                Address = iam.Assignment.Issue.AddressText,
                Status = iam.Assignment.Issue.Status.StatusName,
                Priority = iam.Assignment.Issue.Priority.SeverityRank,
                AssignedAt = iam.AssignedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<StaffTaskResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<PagedResponse<StaffAssignmentResponse>> GetMyPendingAssignmentsAsync(GetMyAssignmentsRequest request, string staffId, CancellationToken cancellationToken = default)
    {
        var query = _context.IssueAssignmentMembers
            .AsNoTracking()
            .Where(iam => iam.UserId == staffId && iam.Status == AssignmentMemberStatus.Pending)
            .Include(iam => iam.Assignment)
                .ThenInclude(a => a.Issue)
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(iam => iam.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(iam => new StaffAssignmentResponse
            {
                AssignmentId = iam.AssignmentId,
                IssueId = iam.Assignment.Issue.IssueId,
                IssueTitle = iam.Assignment.Issue.Title,
                AssignedByManagerName = iam.AssignedBy,
                AssignedAt = iam.AssignedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<StaffAssignmentResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task RespondToAssignmentAsync(long assignmentId, RespondToAssignmentRequest request, string staffId, CancellationToken cancellationToken = default)
    {
        var member = await _context.IssueAssignmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(iam => iam.AssignmentId == assignmentId && iam.UserId == staffId, cancellationToken);

        if (member == null) throw new KeyNotFoundException("Không tìm thấy phân công hoặc bạn không có quyền phản hồi.");

        if (member.Status != AssignmentMemberStatus.Pending)
        {
            throw new InvalidOperationException("Phân công này đã được phản hồi trước đó.");
        }

        var updateRequest = new Application.DTOs.IssueAssignmentMembers.UpdateMemberStatusRequest
        {
            Status = request.Accepted ? AssignmentMemberStatus.Accepted : AssignmentMemberStatus.Rejected,
            Note = request.Accepted ? null : request.RejectionReason
        };

        await _issueAssignmentMemberService.UpdateMemberStatusAsync(assignmentId, updateRequest, staffId, cancellationToken);
    }

    public async Task<PagedResponse<StaffActivityResponse>?> GetMyRecentActivitiesAsync(PagedRequest request, string staffId, CancellationToken cancellationToken = default)
    {
        var staffDepartment = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(dm => dm.UserId == staffId && dm.IsActive, cancellationToken);

        if (staffDepartment == null) return null;

        var departmentId = staffDepartment.DepartmentId;

        var query = _context.IssueUpdates
            .AsNoTracking()
            .Where(u => u.Issue.Assignments.Any(ia => ia.DepartmentId == departmentId && ia.IsCurrent))
            .Include(u => u.Issue)
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new StaffActivityResponse
            {
                IssueId = u.IssueId,
                IssueTitle = u.Issue.Title,
                Description = u.Note,
                ActorName = u.CreatedBy,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<StaffActivityResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }
    
    public async Task<PagedResponse<StaffIncidentResponse>?> GetIncidentsAsync(SearchStaffIncidentsRequest request, string staffId, CancellationToken cancellationToken = default)
    {
        var staffDepartment = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(dm => dm.UserId == staffId && dm.IsActive, cancellationToken);

        if (staffDepartment == null) return null;

        var departmentId = staffDepartment.DepartmentId;

        var query = _context.Issues
            .AsNoTracking()
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.Assignments.Where(ia => ia.IsCurrent))
                .ThenInclude(ia => ia.Members)
            .Where(i => i.Assignments.Any(ia => ia.DepartmentId == departmentId && ia.IsCurrent))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(i => i.Status.StatusCode == request.Status.ToUpperInvariant());
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(i => i.Priority.PriorityId == request.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var keyword = request.Query.Trim().ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(keyword) ||
                i.Description.ToLower().Contains(keyword) ||
                i.PublicCode.ToLower().Contains(keyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new StaffIncidentResponse
            {
                IssueId = i.IssueId,
                Title = i.Title,
                Address = i.AddressText,
                Status = i.Status.StatusName,
                Priority = i.Priority.SeverityRank,
                ReportedAt = i.ReportedAt,
                AssigneeName = i.Assignments
                    .Where(ia => ia.IsCurrent)
                    .SelectMany(ia => ia.Members)
                    .Where(iam => iam.Status == AssignmentMemberStatus.Accepted || iam.Status == AssignmentMemberStatus.Pending)
                    .Select(iam => iam.UserId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<StaffIncidentResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<StaffIncidentDetailResponse?> GetIncidentDetailAsync(long issueId, string staffId, CancellationToken cancellationToken = default)
    {
        var staffDepartment = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(dm => dm.UserId == staffId && dm.IsActive, cancellationToken);

        if (staffDepartment == null) return null;

        var departmentId = staffDepartment.DepartmentId;

        var issue = await _context.Issues
            .AsNoTracking()
            .Include(i => i.IssueType)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Sla)
            .Include(i => i.Assignments.Where(ia => ia.IsCurrent))
                .ThenInclude(ia => ia.Department)
            .Include(i => i.Assignments.Where(ia => ia.IsCurrent))
                .ThenInclude(ia => ia.Members)
            .FirstOrDefaultAsync(i => i.IssueId == issueId &&
                                      i.Assignments.Any(ia => ia.DepartmentId == departmentId && ia.IsCurrent),
                                 cancellationToken);

        if (issue == null) return null;

        var currentAssignment = issue.Assignments.FirstOrDefault(ia => ia.IsCurrent);
        var assigneeName = currentAssignment?.Members
            .Where(iam => iam.Status == AssignmentMemberStatus.Accepted || iam.Status == AssignmentMemberStatus.Pending)
            .Select(iam => iam.UserId)
            .FirstOrDefault();

        var timelineResponse = await _issueService.GetIssueTimelineAsync(issueId, cancellationToken);

        return new StaffIncidentDetailResponse
        {
            IssueId = issue.IssueId,
            ReportId = issue.ReportId,
            Title = issue.Title,
            Description = issue.Description,
            Address = issue.AddressText,
            Latitude = (double)issue.Latitude,
            Longitude = (double)issue.Longitude,
            Status = issue.Status.StatusName,
            Priority = issue.Priority.SeverityRank,
            ReportedAt = issue.ReportedAt,
            ReporterName = issue.ReporterId,
            DepartmentName = currentAssignment?.Department?.DepartmentName ?? "Chưa phân công",
            AssigneeName = assigneeName,
            ImageUrls = issue.Attachments.Select(a => a.FileUrl).ToList(),
            Timeline = timelineResponse.Data ?? new List<IssueTimelineItemResponse>()
        };
    }

    public async Task<IReadOnlyList<StaffMapIssueResponse>?> GetMapIssuesAsync(GetMapIssuesRequest request, string staffId, CancellationToken cancellationToken = default)
    {
        var staffDepartment = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(dm => dm.UserId == staffId && dm.IsActive, cancellationToken);

        if (staffDepartment == null) return null;

        var departmentId = staffDepartment.DepartmentId;

        var minLat = (decimal)request.SouthWestLat;
        var maxLat = (decimal)request.NorthEastLat;
        var minLng = (decimal)request.SouthWestLng;
        var maxLng = (decimal)request.NorthEastLng;

        var query = _context.Issues
            .AsNoTracking()
            .Where(i => i.Latitude >= minLat && i.Latitude <= maxLat &&
                        i.Longitude >= minLng && i.Longitude <= maxLng)
            .Where(i => i.Assignments.Any(ia => ia.DepartmentId == departmentId && ia.IsCurrent))
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(i => i.Status.StatusCode == request.Status.ToUpperInvariant());
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(i => i.Priority.PriorityId == request.Priority.Value);
        }

        if (request.AssignedToMe == true)
        {
            query = query.Where(i => i.Assignments
                .Any(ia => ia.IsCurrent && ia.Members.Any(m => m.UserId == staffId && m.Status == AssignmentMemberStatus.Accepted)));
        }

        var items = await query
            .Take(500)
            .Select(i => new StaffMapIssueResponse
            {
                IssueId = i.IssueId,
                Latitude = (double)i.Latitude,
                Longitude = (double)i.Longitude,
                Status = i.Status.StatusName,
                Priority = i.Priority.SeverityRank
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task ClaimIncidentAsync(long issueId, string staffId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var issue = await _context.Issues
                .Include(i => i.Status)
                .Include(i => i.Assignments.Where(ia => ia.IsCurrent))
                    .ThenInclude(ia => ia.Members)
                .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

            if (issue == null)
            {
                throw new KeyNotFoundException("Không tìm thấy sự cố.");
            }

            if (issue.Status.StatusCode != "PENDING" && issue.Status.StatusCode != "ROUTED")
            {
                throw new InvalidOperationException("Sự cố này không ở trạng thái có thể nhận xử lý.");
            }

            var currentAssignment = issue.Assignments.FirstOrDefault(ia => ia.IsCurrent);
            if (currentAssignment == null) throw new InvalidOperationException("Sự cố chưa được phân công cho phòng ban nào.");

            if (currentAssignment.Members.Any(m => m.Status == AssignmentMemberStatus.Accepted))
            {
                throw new InvalidOperationException("Sự cố đã được nhân viên khác nhận xử lý.");
            }

            var member = currentAssignment.Members.FirstOrDefault(m => m.UserId == staffId);
            if (member == null)
            {
                member = new IssueAssignmentMember
                {
                    AssignmentId = currentAssignment.AssignmentId,
                    UserId = staffId,
                    AssignedBy = staffId,
                    AssignedAt = DateTime.UtcNow,
                    Status = AssignmentMemberStatus.Pending,
                    Note = "Tự nhận xử lý"
                };
                _context.IssueAssignmentMembers.Add(member);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var updateRequest = new Application.DTOs.IssueAssignmentMembers.UpdateMemberStatusRequest
            {
                Status = AssignmentMemberStatus.Accepted,
                Note = "Tự nhận xử lý"
            };
            await _issueAssignmentMemberService.UpdateMemberStatusAsync(member.AssignmentId, updateRequest, staffId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new Exception($"Đã xảy ra lỗi trong quá trình nhận sự cố: {ex.Message}", ex);
        }
    }
}