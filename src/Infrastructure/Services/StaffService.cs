using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Staff;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Application.DTOs.Issues;
using System.Collections.Generic;
using System.Linq;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly AppDbContext _context;

    public StaffService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StaffDashboardSummaryResponse> GetDashboardSummaryAsync(string staffUserId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .Include(m => m.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            throw new UnauthorizedAccessException("Người dùng không phải là thành viên đang hoạt động của bất kỳ đơn vị nào.");
        }

        var departmentId = memberInfo.DepartmentId;

        // 2. Lấy ID của các sự cố đang được phân công cho đơn vị này
        var assignedIssueIdsQuery = _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.IssueId);

        // 3. Lấy ID của trạng thái khởi tạo (mới)
        var initialStatus = await _context.IssueStatuses
            .AsNoTracking()
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (initialStatus is null)
        {
            throw new InvalidOperationException("Hệ thống chưa cấu hình trạng thái sự cố.");
        }

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // 4. Thực hiện các truy vấn đếm
        var issuesInDepartmentQuery = _context.Issues.Where(i => assignedIssueIdsQuery.Contains(i.IssueId));

        var openIssuesQuery = issuesInDepartmentQuery.Where(i => !i.Status.IsClosed);

        var totalOpenIssues = await openIssuesQuery.CountAsync(cancellationToken);
        var newIssues = await openIssuesQuery.CountAsync(i => i.StatusId == initialStatus.StatusId, cancellationToken);
        var inProgressIssues = await openIssuesQuery.CountAsync(i => i.StatusId != initialStatus.StatusId, cancellationToken);
        var recentlyResolvedIssues = await issuesInDepartmentQuery
            .CountAsync(i => i.Status.IsClosed && i.ResolvedAt.HasValue && i.ResolvedAt.Value >= sevenDaysAgo, cancellationToken);
        var slaBreachedOpenIssues = await openIssuesQuery
            .CountAsync(i => i.Sla != null && i.Sla.IsResolutionBreached, cancellationToken);

        // 5. Tạo đối tượng response
        return new StaffDashboardSummaryResponse
        {
            DepartmentId = departmentId,
            DepartmentName = memberInfo.Department.DepartmentName,
            TotalOpenIssues = totalOpenIssues,
            NewIssues = newIssues,
            InProgressIssues = inProgressIssues,
            RecentlyResolvedIssues = recentlyResolvedIssues,
            SlaBreachedOpenIssues = slaBreachedOpenIssues
        };
    }

    public async Task<List<StaffTaskResponse>> GetMyTasksAsync(string staffUserId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            // Trả về danh sách rỗng nếu user không thuộc đơn vị nào.
            // Đây không phải là lỗi, chỉ là họ không có task nào.
            return [];
        }

        var departmentId = memberInfo.DepartmentId;

        // 2. Lấy ID của các sự cố đang được phân công cho đơn vị này
        var assignedIssueIdsQuery = _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.IssueId);

        // 3. Lấy các sự cố đang mở (chưa đóng)
        var tasks = await _context.Issues
            .Where(i => assignedIssueIdsQuery.Contains(i.IssueId) && !i.Status.IsClosed)
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.Area)
            .Include(i => i.Sla)
            .AsNoTracking()
            // Sắp xếp: vi phạm SLA lên trước, rồi đến ưu tiên cao nhất, rồi đến cũ nhất
            .OrderByDescending(i => i.Sla != null && i.Sla.IsResolutionBreached)
            .ThenBy(i => i.Priority.SeverityRank)
            .ThenBy(i => i.ReportedAt)
            .Select(i => new StaffTaskResponse
            {
                IssueId = i.IssueId,
                PublicCode = i.PublicCode,
                Title = i.Title,
                Status = new LookupItemResponse { Id = i.Status.StatusId, Code = i.Status.StatusCode, Name = i.Status.StatusName },
                Priority = new LookupItemResponse { Id = i.Priority.PriorityId, Code = i.Priority.PriorityCode, Name = i.Priority.PriorityName },
                Area = new LookupItemResponse { Id = i.Area.AreaId, Code = i.Area.AreaCode, Name = i.Area.AreaName },
                AddressText = i.AddressText,
                ReportedAt = i.ReportedAt,
                SlaResolutionDueAt = i.Sla != null ? i.Sla.ResolutionDueAt : null,
                IsSlaBreached = i.Sla != null && i.Sla.IsResolutionBreached
            })
            .ToListAsync(cancellationToken);

        return tasks;
    }

    public async Task<List<StaffActivityResponse>> GetMyRecentActivitiesAsync(string staffUserId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            // Không phải là thành viên của đơn vị nào -> không có hoạt động
            return [];
        }

        var departmentId = memberInfo.DepartmentId;
        const int activityLimit = 50;

        // 2. Lấy ID của các sự cố đang được phân công cho đơn vị này (dưới dạng subquery)
        var assignedIssueIdsQuery = _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.IssueId);

        // 3. Lấy các hoạt động gần đây từ các sự cố đó
        var activities = await (from u in _context.IssueUpdates
                                join actor in _context.Users on u.CreatedBy equals actor.Id into actorGroup
                                from act in actorGroup.DefaultIfEmpty() // LEFT JOIN to handle system-generated notes
                                where assignedIssueIdsQuery.Contains(u.IssueId)
                                select new
                                {
                                    Update = u,
                                    Issue = u.Issue,
                                    ActorName = act != null ? act.FullName : "Hệ thống"
                                })
            .AsNoTracking()
            .OrderByDescending(x => x.Update.CreatedAt)
            .Take(activityLimit)
            .Select(x => new StaffActivityResponse
            {
                ActivityId = x.Update.Id,
                ActivityType =
                    x.Update.FromStatusId == null ? "CREATED" :
                    (x.Update.Note != null && (x.Update.Note.StartsWith("Đã chuyển đơn vị") || x.Update.Note.StartsWith("Đã định tuyến"))) ? "ASSIGNMENT" :
                    x.Update.FromStatusId != x.Update.ToStatusId ? "STATUS_CHANGE" :
                    "COMMENT",
                ActivityTimestamp = x.Update.CreatedAt,
                IssueId = x.Update.IssueId,
                IssuePublicCode = x.Issue.PublicCode,
                IssueTitle = x.Issue.Title,
                Description = x.Update.Note ?? string.Empty,
                ActorName = x.ActorName
            })
            .ToListAsync(cancellationToken);

        return activities;
    }

    public async Task<PagedResponse<StaffIncidentResponse>> GetIncidentsAsync(string staffUserId, StaffIncidentFilterRequest filters, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            // Nếu không thuộc đơn vị nào, không có quyền xem sự cố. Trả về kết quả rỗng.
            return new PagedResponse<StaffIncidentResponse>
            {
                Items = [], Page = filters.Page, PageSize = filters.PageSize, TotalItems = 0, TotalPages = 0
            };
        }
        var departmentId = memberInfo.DepartmentId;

        // 2. Base query với LEFT JOIN để lấy thông tin phân công hiện tại
        var query = from issue in _context.Issues
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Sla)
            join assignment in _context.IssueAssignments.Include(a => a.Department).Where(a => a.IsCurrent)
                on issue.IssueId equals assignment.IssueId into gj
            from currentAssignment in gj.DefaultIfEmpty()
            select new { issue, currentAssignment };

        // 3. Áp dụng phạm vi bảo mật: Cán bộ chỉ thấy sự cố của đơn vị mình và các sự cố chưa được phân công.
        query = query.Where(x => x.currentAssignment == null || x.currentAssignment.DepartmentId == departmentId);

        // 4. Áp dụng các bộ lọc từ request
        switch (filters.Assignment?.ToLowerInvariant())
        {
            case "mine":
                query = query.Where(x => x.currentAssignment != null && x.currentAssignment.DepartmentId == departmentId);
                break;
            case "unassigned":
                query = query.Where(x => x.currentAssignment == null);
                break;
        }

        if (!string.IsNullOrWhiteSpace(filters.Keyword))
        {
            var keyword = filters.Keyword.Trim().ToLower();
            query = query.Where(x => x.issue.PublicCode.ToLower().Contains(keyword) || x.issue.Title.ToLower().Contains(keyword));
        }

        if (filters.StatusCodes?.Length > 0)
        {
            query = query.Where(x => filters.StatusCodes.Contains(x.issue.Status.StatusCode));
        }

        if (filters.PriorityIds?.Length > 0)
        {
            query = query.Where(x => filters.PriorityIds.Contains(x.issue.PriorityId));
        }

        if (filters.IssueTypeIds?.Length > 0)
        {
            query = query.Where(x => filters.IssueTypeIds.Contains(x.issue.IssueTypeId));
        }

        // 5. Phân trang
        var totalItems = await query.CountAsync(cancellationToken);
        var page = filters.Page < 1 ? 1 : filters.Page;
        var pageSize = filters.PageSize < 1 ? 20 : filters.PageSize;

        var pagedQuery = query
            .OrderByDescending(x => x.issue.Sla != null && x.issue.Sla.IsResolutionBreached)
            .ThenBy(x => x.issue.Priority.SeverityRank)
            .ThenBy(x => x.issue.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        // 6. Project sang DTO
        var items = await pagedQuery.Select(x => new StaffIncidentResponse
        {
            IssueId = x.issue.IssueId,
            PublicCode = x.issue.PublicCode,
            Title = x.issue.Title,
            Status = new LookupItemResponse { Id = x.issue.Status.StatusId, Code = x.issue.Status.StatusCode, Name = x.issue.Status.StatusName },
            Priority = new LookupItemResponse { Id = x.issue.Priority.PriorityId, Code = x.issue.Priority.PriorityCode, Name = x.issue.Priority.PriorityName },
            IssueType = new LookupItemResponse { Id = x.issue.IssueType.IssueTypeId, Code = x.issue.IssueType.TypeCode, Name = x.issue.IssueType.TypeName },
            Area = new LookupItemResponse { Id = x.issue.Area.AreaId, Code = x.issue.Area.AreaCode, Name = x.issue.Area.AreaName },
            ReportedAt = x.issue.ReportedAt,
            AssignedDepartment = x.currentAssignment != null ? new LookupItemResponse { Id = x.currentAssignment.Department.DepartmentId, Code = x.currentAssignment.Department.DepartmentCode, Name = x.currentAssignment.Department.DepartmentName } : null,
            IsSlaBreached = x.issue.Sla != null && x.issue.Sla.IsResolutionBreached
        }).ToListAsync(cancellationToken);

        return new PagedResponse<StaffIncidentResponse>
        {
            Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems, TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };
    }

    public async Task<List<StaffMapIssueResponse>> GetMapIssuesAsync(string staffUserId, StaffIncidentFilterRequest filters, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            // Nếu không thuộc đơn vị nào, không có quyền xem sự cố. Trả về kết quả rỗng.
            return [];
        }
        var departmentId = memberInfo.DepartmentId;

        // 2. Base query với LEFT JOIN để lấy thông tin phân công hiện tại
        var query = from issue in _context.Issues
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.IssueType)
            .Include(i => i.Sla)
            join assignment in _context.IssueAssignments.Include(a => a.Department).Where(a => a.IsCurrent)
                on issue.IssueId equals assignment.IssueId into gj
            from currentAssignment in gj.DefaultIfEmpty()
            select new { issue, currentAssignment };

        // 3. Áp dụng phạm vi bảo mật: Cán bộ chỉ thấy sự cố của đơn vị mình và các sự cố chưa được phân công.
        query = query.Where(x => x.currentAssignment == null || x.currentAssignment.DepartmentId == departmentId);

        // 4. Áp dụng các bộ lọc từ request
        switch (filters.Assignment?.ToLowerInvariant())
        {
            case "mine":
                query = query.Where(x => x.currentAssignment != null && x.currentAssignment.DepartmentId == departmentId);
                break;
            case "unassigned":
                query = query.Where(x => x.currentAssignment == null);
                break;
        }

        if (!string.IsNullOrWhiteSpace(filters.Keyword))
        {
            var keyword = filters.Keyword.Trim().ToLower();
            query = query.Where(x => x.issue.PublicCode.ToLower().Contains(keyword) || x.issue.Title.ToLower().Contains(keyword));
        }

        if (filters.StatusCodes?.Length > 0)
        {
            query = query.Where(x => filters.StatusCodes.Contains(x.issue.Status.StatusCode));
        }

        if (filters.PriorityIds?.Length > 0)
        {
            query = query.Where(x => filters.PriorityIds.Contains(x.issue.PriorityId));
        }

        if (filters.IssueTypeIds?.Length > 0)
        {
            query = query.Where(x => filters.IssueTypeIds.Contains(x.issue.IssueTypeId));
        }

        // 5. Project sang DTO và thực thi query (không phân trang)
        var items = await query
            .OrderByDescending(x => x.issue.ReportedAt) // Sắp xếp đơn giản cho map
            .Select(x => new StaffMapIssueResponse
            {
                IssueId = x.issue.IssueId,
                PublicCode = x.issue.PublicCode,
                Title = x.issue.Title,
                Latitude = x.issue.Latitude,
                Longitude = x.issue.Longitude,
                Status = new LookupItemResponse { Id = x.issue.Status.StatusId, Code = x.issue.Status.StatusCode, Name = x.issue.Status.StatusName },
                Priority = new LookupItemResponse { Id = x.issue.Priority.PriorityId, Code = x.issue.Priority.PriorityCode, Name = x.issue.Priority.PriorityName },
                IssueType = new LookupItemResponse { Id = x.issue.IssueType.IssueTypeId, Code = x.issue.IssueType.TypeCode, Name = x.issue.IssueType.TypeName },
                AssignedDepartment = x.currentAssignment != null ? new LookupItemResponse { Id = x.currentAssignment.Department.DepartmentId, Code = x.currentAssignment.Department.DepartmentCode, Name = x.currentAssignment.Department.DepartmentName } : null,
                IsSlaBreached = x.issue.Sla != null && x.issue.Sla.IsResolutionBreached
            }).ToListAsync(cancellationToken);

        return items;
    }

    public async Task ClaimIncidentAsync(long issueId, string staffUserId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị và user của cán bộ
        var memberAndUser = await (from member in _context.DepartmentMembers
                                   join user in _context.Users on member.UserId equals user.Id
                                   where member.UserId == staffUserId && member.IsActive
                                   select new { Member = member, User = user })
                                  .Include(x => x.Member.Department)
                                  .FirstOrDefaultAsync(cancellationToken);

        if (memberAndUser?.User is null || memberAndUser.Member?.Department is null)
        {
            throw new UnauthorizedAccessException("Bạn không phải là thành viên đang hoạt động của bất kỳ đơn vị nào để nhận sự cố.");
        }

        var memberInfo = memberAndUser.Member;
        var actorUser = memberAndUser.User;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // 2. Lấy sự cố và kiểm tra
        var issue = await _context.Issues
            .Include(i => i.Sla)
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

        if (issue is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sự cố với ID = {issueId}.");
        }

        var isAlreadyAssigned = await _context.IssueAssignments
            .AnyAsync(a => a.IssueId == issueId && a.IsCurrent, cancellationToken);

        if (isAlreadyAssigned)
        {
            throw new InvalidOperationException("Sự cố này đã được phân công cho một đơn vị khác.");
        }

        var now = DateTime.UtcNow;

        // 3. Tạo bản ghi phân công mới
        var newAssignment = new Domain.Entities.IssueAssignment
        {
            IssueId = issueId,
            DepartmentId = memberInfo.DepartmentId,
            AssignmentMethod = "MANUAL_CLAIM",
            AssignmentNote = $"Được nhận bởi cán bộ {actorUser.FullName}.",
            AssignedAt = now,
            IsCurrent = true
        };
        _context.IssueAssignments.Add(newAssignment);

        // 4. Tạo bản ghi timeline
        var timelineUpdate = new Domain.Entities.IssueUpdate
        {
            IssueId = issueId,
            CreatedBy = staffUserId,
            FromStatusId = issue.StatusId,
            ToStatusId = issue.StatusId,
            Note = $"Đơn vị '{memberInfo.Department.DepartmentName}' đã tiếp nhận xử lý (Cán bộ: {actorUser.FullName}).",
            IsSystemGenerated = false,
            CreatedAt = now
        };
        _context.IssueUpdates.Add(timelineUpdate);

        // 5. Cập nhật SLA (ghi nhận first response)
        if (issue.Sla is { FirstRespondedAt: null })
        {
            issue.Sla.FirstRespondedAt = now;
            if (issue.Sla.FirstRespondedAt > issue.Sla.FirstResponseDueAt)
            {
                issue.Sla.IsFirstResponseBreached = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}