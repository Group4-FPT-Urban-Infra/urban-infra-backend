using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using UrbanInfraSystem.Application.DTOs;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Staff;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public StaffService(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
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

        // 2. Base query với LEFT JOIN để lấy thông tin phân công hiện tại và thành viên được giao
        var query = from issue in _context.Issues
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Sla)
            join assignment in _context.IssueAssignments
                .Include(a => a.Department)
                .Include(a => a.Members)
                .Where(a => a.IsCurrent)
                on issue.IssueId equals assignment.IssueId into gj
            from currentAssignment in gj.DefaultIfEmpty()
            select new { issue, currentAssignment };

        // 3. Áp dụng phạm vi bảo mật ban đầu
        // Base query: chỉ thấy sự cố của đơn vị mình hoặc chưa được phân công
        query = query.Where(x => x.currentAssignment == null || x.currentAssignment.DepartmentId == departmentId);

        // 4. Áp dụng các bộ lọc từ request
        var scope = filters.Scope?.ToLowerInvariant() ?? "department";
        switch (scope)
        {
            case "my":
                // Chỉ hiển thị issue đã được phân công cho staff hiện tại
                query = query.Where(x =>
                    x.currentAssignment != null &&
                    x.currentAssignment.DepartmentId == departmentId &&
                    x.currentAssignment.Members.Any(m => m.UserId == staffUserId));
                break;
            case "all":
                // Hiển thị tất cả issue (trừ rejected)
                query = query.Where(x => x.issue.Status.StatusCode != "REJECTED");
                break;
            case "department":
            default:
                // Giữ nguyên filter bảo mật ban đầu
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
            AssignmentStatus = x.currentAssignment != null && x.currentAssignment.Members.Any(m => m.UserId == staffUserId)
                ? x.currentAssignment.Members.Where(m => m.UserId == staffUserId).Select(m => m.Status).FirstOrDefault()
                : null,
            IsSlaBreached = x.issue.Sla != null && x.issue.Sla.IsResolutionBreached
        }).ToListAsync(cancellationToken);

        return new PagedResponse<StaffIncidentResponse>
        {
            Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems, TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };
    }

    public async Task<List<StaffMapIssueResponse>> GetMapIssuesAsync(string staffUserId, StaffMapFilterRequest filters, CancellationToken cancellationToken)
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

        // 3. Áp dụng phạm vi lọc dựa trên Scope
        var scope = filters.Scope?.ToLowerInvariant() ?? "department";
        switch (scope)
        {
            case "my":
                // Chỉ hiển thị issue của đơn vị mình (giống department)
                query = query.Where(x => x.currentAssignment != null && x.currentAssignment.DepartmentId == departmentId);
                break;
            case "all":
                // Hiển thị tất cả issue trong hệ thống (hoặc giới hạn bán kính)
                // Không cần thêm filter, chỉ cần đảm bảo không thấy issue bị từ chối
                query = query.Where(x => !x.issue.Status.IsClosed || x.issue.Status.StatusCode != "REJECTED");
                break;
            case "department":
            default:
                // Issue của đơn vị hoặc chưa được phân công
                query = query.Where(x => x.currentAssignment == null || x.currentAssignment.DepartmentId == departmentId);
                break;
        }

        // 4. Áp dụng các bộ lọc từ request
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

        // 5. Filter theo bán kính nếu có
        if (filters.RadiusMeters.HasValue && filters.Latitude.HasValue && filters.Longitude.HasValue)
        {
            var centerLat = filters.Latitude.Value;
            var centerLng = filters.Longitude.Value;
            var radiusMeters = filters.RadiusMeters.Value;

            // Sử dụng Haversine formula approximation để lọc
            // 1 độ latitude ≈ 111km, 1 độ longitude ≈ 111km * cos(latitude)
            var latDegrees = radiusMeters / 111000.0;
            var lngDegrees = radiusMeters / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0));

            query = query.Where(x =>
                Math.Abs((double)x.issue.Latitude - centerLat) <= latDegrees &&
                Math.Abs((double)x.issue.Longitude - centerLng) <= lngDegrees);
        }

        // 6. Chỉ lấy các issue có tọa độ
        query = query.Where(x => x.issue.Latitude != 0 && x.issue.Longitude != 0);

        // 7. Project sang DTO và thực thi query (không phân trang)
        var items = await query
            .OrderByDescending(x => x.issue.Sla != null && x.issue.Sla.IsResolutionBreached)
            .ThenBy(x => x.issue.Priority.SeverityRank)
            .ThenByDescending(x => x.issue.ReportedAt)
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

        // 8. Nếu có bán kính, lọc chính xác hơn bằng Haversine (post-query filter)
        if (filters.RadiusMeters.HasValue && filters.Latitude.HasValue && filters.Longitude.HasValue)
        {
            var centerLat = filters.Latitude.Value;
            var centerLng = filters.Longitude.Value;
            var radiusMeters = filters.RadiusMeters.Value;

            items = items.Where(i =>
            {
                var distance = CalculateHaversineDistance(centerLat, centerLng, (double)i.Latitude, (double)i.Longitude);
                return distance <= radiusMeters;
            }).ToList();
        }

        return items;
    }

    private static double CalculateHaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000; // Bán kính trái đất tính bằng mét
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
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

    public async Task<StaffIncidentDetailResponse?> GetIncidentDetailAsync(long issueId, string staffUserId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            return null;
        }
        var departmentId = memberInfo.DepartmentId;

        // 2. Lấy thông tin issue với các navigation properties
        var issue = await _context.Issues
            .Include(i => i.Report)
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Sla)
            .Include(i => i.Assignments.Where(a => a.IsCurrent)).ThenInclude(a => a.Department)
            .Include(i => i.Assignments.Where(a => a.IsCurrent)).ThenInclude(a => a.Members)
            .Include(i => i.Updates.OrderByDescending(u => u.CreatedAt)).ThenInclude(u => u.FromStatus)
            .Include(i => i.Updates.OrderByDescending(u => u.CreatedAt)).ThenInclude(u => u.ToStatus)
            .Include(i => i.Updates.OrderByDescending(u => u.CreatedAt)).ThenInclude(u => u.Attachments)
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

        if (issue is null)
        {
            return null;
        }

        // 3. Kiểm tra quyền truy cập
        var currentAssignment = issue.Assignments.FirstOrDefault();
        if (currentAssignment != null && currentAssignment.DepartmentId != departmentId)
        {
            return null;
        }

        // 4. Lấy reporter info
        var reporter = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == issue.ReporterId, cancellationToken);

        // 5. Phân tách images: reporter vs staff
        var allAttachments = issue.Updates
            .SelectMany(u => u.Attachments.Select(a => new { Attachment = a, Update = u }))
            .ToList();

        var reporterImages = allAttachments
            .Where(x => x.Update.CreatedBy == issue.ReporterId)
            .Select(x => new IssueAttachmentResponse
            {
                Id = x.Attachment.Id,
                Kind = x.Attachment.Kind,
                FileUrl = x.Attachment.FileUrl,
                ThumbnailUrl = x.Attachment.ThumbnailUrl,
                MimeType = x.Attachment.MimeType,
                FileSizeBytes = x.Attachment.FileSizeBytes,
                WidthPx = x.Attachment.WidthPx,
                HeightPx = x.Attachment.HeightPx,
                CreatedAt = x.Attachment.CreatedAt
            })
            .ToList();

        var staffImages = allAttachments
            .Where(x => x.Update.CreatedBy != issue.ReporterId)
            .Select(x => new IssueAttachmentResponse
            {
                Id = x.Attachment.Id,
                Kind = x.Attachment.Kind,
                FileUrl = x.Attachment.FileUrl,
                ThumbnailUrl = x.Attachment.ThumbnailUrl,
                MimeType = x.Attachment.MimeType,
                FileSizeBytes = x.Attachment.FileSizeBytes,
                WidthPx = x.Attachment.WidthPx,
                HeightPx = x.Attachment.HeightPx,
                CreatedAt = x.Attachment.CreatedAt
            })
            .ToList();

        // 6. Build timeline
        var timeline = issue.Updates
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new IssueTimelineItemResponse
            {
                Id = u.Id,
                UpdateType = u.FromStatusId == null ? "CREATED" : (u.FromStatusId != u.ToStatusId ? "STATUS_CHANGE" : "COMMENT"),
                FromStatus = u.FromStatus != null ? new LookupItemResponse { Id = u.FromStatus.StatusId, Code = u.FromStatus.StatusCode, Name = u.FromStatus.StatusName } : null,
                ToStatus = u.ToStatus != null ? new LookupItemResponse { Id = u.ToStatus.StatusId, Code = u.ToStatus.StatusCode, Name = u.ToStatus.StatusName } : null,
                Note = u.Note,
                IsSystemGenerated = u.IsSystemGenerated,
                CreatedAt = u.CreatedAt,
                Attachments = u.Attachments.Select(a => new IssueAttachmentResponse
                {
                    Id = a.Id,
                    Kind = a.Kind,
                    FileUrl = a.FileUrl,
                    ThumbnailUrl = a.ThumbnailUrl,
                    MimeType = a.MimeType,
                    FileSizeBytes = a.FileSizeBytes,
                    WidthPx = a.WidthPx,
                    HeightPx = a.HeightPx,
                    CreatedAt = a.CreatedAt
                }).ToList()
            })
            .ToList();

            // 7. Build current member info
            var staffMember = currentAssignment?.Members.FirstOrDefault(m => m.UserId == staffUserId);
            CurrentMemberInfo? currentMemberInfo = null;
            if (staffMember != null)
            {
                currentMemberInfo = new CurrentMemberInfo
                {
                    MemberId = staffMember.MemberId,
                    Status = staffMember.Status
                };
            }

            // 8. Build assignment info
            AssignmentInfoResponse? assignmentInfo = null;
            if (currentAssignment != null)
            {
                var assigneeName = staffMember != null
                    ? await _context.Users.Where(u => u.Id == staffMember.UserId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
                    : null;
                assignmentInfo = new AssignmentInfoResponse
                {
                    Department = new LookupItemResponse
                    {
                        Id = currentAssignment.Department.DepartmentId,
                        Code = currentAssignment.Department.DepartmentCode,
                        Name = currentAssignment.Department.DepartmentName
                    },
                    AssigneeName = assigneeName,
                    AssignmentStatus = staffMember?.Status
                };
            }

            // 9. Build response
            return new StaffIncidentDetailResponse
            {
                IssueId = issue.IssueId,
                ReportId = issue.ReportId,
                PublicCode = issue.PublicCode,
                Title = issue.Title,
                Description = issue.Description,
                Address = issue.AddressText,
                Latitude = (double)issue.Latitude,
                Longitude = (double)issue.Longitude,
                Status = new LookupItemResponse { Id = issue.Status.StatusId, Code = issue.Status.StatusCode, Name = issue.Status.StatusName },
                Priority = new LookupItemResponse { Id = issue.Priority.PriorityId, Code = issue.Priority.PriorityCode, Name = issue.Priority.PriorityName },
                IssueType = new LookupItemResponse { Id = issue.IssueType.IssueTypeId, Code = issue.IssueType.TypeCode, Name = issue.IssueType.TypeName },
                Area = new LookupItemResponse { Id = issue.Area.AreaId, Code = issue.Area.AreaCode, Name = issue.Area.AreaName },
                ReportedAt = issue.ReportedAt,
                IsSlaBreached = issue.Sla?.IsResolutionBreached ?? false,
                Reporter = new ReporterInfoResponse
                {
                    DisplayName = reporter?.FullName ?? "Unknown",
                    PhoneNumber = reporter?.PhoneNumber,
                    Email = reporter?.Email
                },
                CurrentMember = currentMemberInfo,
                Assignment = assignmentInfo,
                ReporterImages = reporterImages,
                StaffImages = staffImages,
                Timeline = timeline
            };
        }

    public async Task<StaffIncidentDetailResponse> UpdateIssueAsync(long issueId, StaffUpdateIssueRequest request, string staffUserId, CancellationToken cancellationToken)
    {
        // 1. Lấy thông tin đơn vị của cán bộ
        var memberInfo = await _context.DepartmentMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == staffUserId && m.IsActive, cancellationToken);

        if (memberInfo is null)
        {
            throw new UnauthorizedAccessException("Bạn không phải là thành viên đang hoạt động của bất kỳ đơn vị nào.");
        }
        var departmentId = memberInfo.DepartmentId;

        // 2. Lấy issue
        var issue = await _context.Issues
            .Include(i => i.Status)
            .Include(i => i.Priority)
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Sla)
            .Include(i => i.Assignments.Where(a => a.IsCurrent)).ThenInclude(a => a.Department)
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

        if (issue is null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sự cố với ID = {issueId}.");
        }

        var currentAssignment = issue.Assignments.FirstOrDefault();
        if (currentAssignment == null || currentAssignment.DepartmentId != departmentId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật sự cố này.");
        }

        var now = DateTime.UtcNow;
        int? toStatusId = null;

        // 3. Cập nhật trạng thái nếu có
        if (request.StatusId.HasValue)
        {
            var targetStatus = await _context.IssueStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusId == request.StatusId.Value, cancellationToken);

            if (targetStatus == null)
            {
                throw new ArgumentException($"Không tìm thấy trạng thái với ID = {request.StatusId}.");
            }

            if (targetStatus.StatusId != issue.StatusId)
            {
                issue.StatusId = targetStatus.StatusId;
                issue.Status = targetStatus;
                toStatusId = targetStatus.StatusId;

                if (targetStatus.IsClosed)
                {
                    issue.ResolvedAt ??= now;
                    if (issue.Sla != null)
                    {
                        issue.Sla.ResolvedAt = now;
                    }
                }
            }
        }

        // 4. Tạo IssueUpdate khi có ảnh minh chứng hoặc đổi trạng thái
        var hasImages = request.Images?.Count > 0;
        var hasStatusChange = toStatusId.HasValue;

        if (hasImages || hasStatusChange)
        {
            var update = new IssueUpdate
            {
                IssueId = issueId,
                CreatedBy = staffUserId,
                FromStatusId = hasStatusChange ? issue.StatusId : (int?)null,
                ToStatusId = hasStatusChange ? toStatusId!.Value : issue.StatusId,
                Note = string.IsNullOrWhiteSpace(request.Note)
                    ? $"Nhân viên đã upload minh chứng hoàn thành."
                    : request.Note,
                IsSystemGenerated = false,
                CreatedAt = now
            };
            _context.IssueUpdates.Add(update);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Lưu file ảnh nếu có
            if (hasImages)
            {
                var webRoot = _environment.WebRootPath;
                var uploadDir = Path.Combine(webRoot, "uploads", "issues", "staff", issueId.ToString());

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                foreach (var image in request.Images!)
                {
                    if (image.Length == 0) continue;

                    var extension = Path.GetExtension(image.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await image.CopyToAsync(stream, cancellationToken);

                    var attachment = new IssueAttachment(update.Id)
                    {
                        IssueId = issueId,
                        UploadedBy = staffUserId,
                        Kind = "image",
                        FileUrl = $"/uploads/issues/staff/{issueId}/{uniqueFileName}",
                        MimeType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType,
                        FileSizeBytes = image.Length,
                        CreatedAt = now
                    };
                    _context.IssueAttachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        // 6. Trả về chi tiết đã cập nhật
        return (await GetIncidentDetailAsync(issueId, staffUserId, cancellationToken))!;
    }
}
