using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class DepartmentManagerIssueService : IDepartmentManagerIssueService
{
    private readonly AppDbContext _context;

    public DepartmentManagerIssueService(AppDbContext context) => _context = context;

    public async Task<PaginatedResponse<DepartmentManagerIssueSummary>> GetIssuesAsync(int departmentId, DepartmentManagerIssueListRequest request, CancellationToken cancellationToken = default)
    {
        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        IQueryable<Issue> query = _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Sla)
            .Include(i => i.Attachments)
            .Include(i => i.Assignments)
            .Where(i => _context.IssueAssignments
                .Where(a => currentAssignmentIds.Contains(a.AssignmentId))
                .Select(a => a.IssueId)
                .Contains(i.IssueId));

        List<long> activeAssignmentIds = currentAssignmentIds;

        if (request.Filter == "unassigned")
        {
            var assignedAssignmentIds = await _context.IssueAssignmentMembers
                .Where(m => currentAssignmentIds.Contains(m.AssignmentId) && m.Status != AssignmentMemberStatus.Rejected)
                .Select(m => m.AssignmentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            activeAssignmentIds = currentAssignmentIds.Except(assignedAssignmentIds).ToList();

            query = _context.Issues
                .Include(i => i.IssueType)
                .Include(i => i.Priority)
                .Include(i => i.Status)
                .Include(i => i.Sla)
                .Include(i => i.Attachments)
                .Include(i => i.Assignments)
                .Where(i => _context.IssueAssignments
                    .Where(a => activeAssignmentIds.Contains(a.AssignmentId))
                    .Select(a => a.IssueId)
                    .Contains(i.IssueId));
        }
        else if (request.Filter == "team_assigned")
        {
            activeAssignmentIds = await _context.IssueAssignmentMembers
                .Where(m => currentAssignmentIds.Contains(m.AssignmentId) && m.Status != AssignmentMemberStatus.Rejected)
                .Select(m => m.AssignmentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = _context.Issues
                .Include(i => i.IssueType)
                .Include(i => i.Priority)
                .Include(i => i.Status)
                .Include(i => i.Sla)
                .Include(i => i.Attachments)
                .Include(i => i.Assignments)
                .Where(i => _context.IssueAssignments
                    .Where(a => activeAssignmentIds.Contains(a.AssignmentId))
                    .Select(a => a.IssueId)
                    .Contains(i.IssueId));
        }

        if (request.StatusIds != null && request.StatusIds.Count > 0)
            query = query.Where(i => request.StatusIds.Contains(i.StatusId));
        if (request.PriorityIds != null && request.PriorityIds.Count > 0)
            query = query.Where(i => request.PriorityIds.Contains(i.PriorityId));
        if (request.IssueTypeIds != null && request.IssueTypeIds.Count > 0)
            query = query.Where(i => request.IssueTypeIds.Contains(i.IssueTypeId));
        if (!string.IsNullOrWhiteSpace(request.Keyword) && request.Filter != "my_assigned")
            query = query.Where(i => i.Title.Contains(request.Keyword) || i.PublicCode.Contains(request.Keyword));

        var totalCount = await query.CountAsync(cancellationToken);
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;

        var issues = await query
            .OrderBy(i => i.Sla == null ? 4 :
                i.Sla.ResolvedAt.HasValue ? 5 :
                i.Sla.ResolutionDueAt < DateTime.UtcNow ? 0 :
                !i.Sla.FirstRespondedAt.HasValue && i.Sla.FirstResponseDueAt.HasValue && i.Sla.FirstResponseDueAt < DateTime.UtcNow ? 1 :
                i.Sla.ResolutionDueAt < DateTime.UtcNow.AddHours(2) ? 2 : 3)
            .ThenBy(i => i.PriorityId)
            .ThenByDescending(i => i.ReportedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var issueIds = issues.Select(i => i.IssueId).ToList();

        var assignmentToMemberCount = new Dictionary<long, int>();
        foreach (var assignmentId in activeAssignmentIds)
        {
            var count = await _context.IssueAssignmentMembers
                .CountAsync(m => m.AssignmentId == assignmentId && m.Status != AssignmentMemberStatus.Rejected, cancellationToken);
            assignmentToMemberCount[assignmentId] = count;
        }

        return new PaginatedResponse<DepartmentManagerIssueSummary>
        {
            Items = issues.Select(i => MapToSummary(i, GetIssueAssignedMemberCount(i, activeAssignmentIds, assignmentToMemberCount))).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static int GetIssueAssignedMemberCount(Issue issue, List<long> activeAssignmentIds, Dictionary<long, int> assignmentToMemberCount)
    {
        var assignmentId = issue.Assignments
            .Where(a => activeAssignmentIds.Contains(a.AssignmentId))
            .Select(a => a.AssignmentId)
            .FirstOrDefault();

        return assignmentId > 0 && assignmentToMemberCount.TryGetValue(assignmentId, out var count) ? count : 0;
    }

    public async Task<DepartmentManagerIssueDetailResponse?> GetIssueDetailAsync(long issueId, CancellationToken cancellationToken = default)
    {
        var issue = await _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Priority)
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

        if (issue is null) return null;

        var currentAssignment = await _context.IssueAssignments
            .Include(a => a.Department)
            .Where(a => a.IssueId == issueId && a.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

        var currentAssignmentId = currentAssignment?.AssignmentId;

        var memberIds = await _context.IssueAssignmentMembers
            .Where(m => m.AssignmentId == currentAssignmentId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var memberUsers = await _context.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var members = await _context.IssueAssignmentMembers
            .Where(m => m.AssignmentId == currentAssignmentId)
            .OrderByDescending(m => m.AssignedAt)
            .ToListAsync(cancellationToken);

        var updates = await _context.IssueUpdates
            .Include(u => u.FromStatus)
            .Include(u => u.ToStatus)
            .Where(u => u.IssueId == issueId)
            .OrderByDescending(u => u.CreatedAt)
            .Take(50)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var updateUserIds = members.Select(m => m.UserId).Append("").Distinct();
        var updateUsers = await _context.Users
            .Where(u => updateUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return new DepartmentManagerIssueDetailResponse
        {
            IssueId = issue.IssueId,
            PublicCode = issue.PublicCode,
            Title = issue.Title,
            Description = issue.Description,
            IssueTypeName = issue.IssueType?.TypeName ?? string.Empty,
            PriorityName = issue.Priority?.PriorityName ?? string.Empty,
            StatusName = issue.Status?.StatusName ?? string.Empty,
            AddressText = issue.AddressText ?? string.Empty,
            Latitude = issue.Latitude,
            Longitude = issue.Longitude,
            ReportedAt = issue.ReportedAt,
            ResolvedAt = issue.ResolvedAt,
            UpvoteCount = issue.UpvoteCount,
            IsPublic = issue.IsPublic,
            ImageUrls = issue.Attachments.Select(a => a.FileUrl).ToList(),
            CurrentAssignment = currentAssignment is null ? null : new CurrentAssignmentInfo
            {
                AssignmentId = currentAssignment.AssignmentId,
                DepartmentId = currentAssignment.DepartmentId,
                DepartmentName = currentAssignment.Department?.DepartmentName ?? string.Empty,
                AssignedAt = currentAssignment.AssignedAt
            },
            AssignedMembers = members.Select(m => new AssignmentMemberInfo
            {
                MemberId = m.MemberId,
                UserId = m.UserId,
                FullName = memberUsers.TryGetValue(m.UserId, out var u) ? u.FullName : string.Empty,
                AvatarUrl = null,
                Status = m.Status,
                AssignedAt = m.AssignedAt,
                AcceptedAt = m.AcceptedAt,
                EndedAt = m.EndedAt,
                Note = m.Note
            }).ToList(),
            Updates = updates.Select(u => new IssueUpdateInfo
            {
                Id = u.Id,
                CreatedByName = updateUsers.TryGetValue(u.CreatedBy, out var creator) ? creator.FullName : u.CreatedBy,
                FromStatusName = u.FromStatus?.StatusName,
                ToStatusName = u.ToStatus?.StatusName ?? string.Empty,
                Note = u.Note,
                ProgressPercent = u.ProgressPercent,
                IsSystemGenerated = u.IsSystemGenerated,
                CreatedAt = u.CreatedAt
            }).ToList()
        };
    }

    public async Task<DepartmentManagerIssueDetailResponse> AssignIssueAsync(long issueId, AssignIssueRequest request, string assignedBy, CancellationToken cancellationToken = default)
    {
        var assignment = await _context.IssueAssignments
            .Include(a => a.Department)
            .Where(a => a.IssueId == issueId && a.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay phan cong hien tai cho su co ID = {issueId}.");

        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay nguoi dung co ID = {request.UserId}.");

        var existingMember = await _context.IssueAssignmentMembers
            .FirstOrDefaultAsync(m => m.AssignmentId == assignment.AssignmentId && m.UserId == request.UserId, cancellationToken);

        if (existingMember is not null)
            throw new InvalidOperationException("Nhan vien nay da duoc gan vao phan cong nay.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var member = new IssueAssignmentMember
        {
            AssignmentId = assignment.AssignmentId,
            UserId = request.UserId,
            AssignedBy = assignedBy,
            AssignedAt = now,
            Status = AssignmentMemberStatus.Pending,
            Note = request.Note
        };

        _context.IssueAssignmentMembers.Add(member);

        var issue = await _context.Issues
            .Include(i => i.Sla)
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay su co co ID = {issueId}.");

        // Cap nhat FirstRespondedAt lan dau tien duoc gan nhan vien
        if (issue.Sla != null && !issue.Sla.FirstRespondedAt.HasValue)
        {
            issue.Sla.FirstRespondedAt = now;
            if (issue.Sla.FirstResponseDueAt.HasValue && now > issue.Sla.FirstResponseDueAt.Value)
            {
                issue.Sla.IsFirstResponseBreached = true;
            }
        }

        _context.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = issueId,
            CreatedBy = assignedBy,
            FromStatusId = issue.StatusId,
            ToStatusId = issue.StatusId,
            Note = $"Da gan nhan vien '{user.FullName}' vao phan cong." + (string.IsNullOrWhiteSpace(request.Note) ? "" : $" Ghi chu: {request.Note}"),
            IsSystemGenerated = false,
            CreatedAt = now
        });

        _context.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Title = "Ban duoc gan xu ly su co",
            Message = $"Ban da duoc gan xu ly su co '{issueId}' trong phan cong cua don vi '{assignment.Department?.DepartmentName}'.",
            NotificationType = "ASSIGNMENT_MEMBER",
            IssueId = issueId,
            IsRead = false,
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await GetIssueDetailAsync(issueId, cancellationToken))!;
    }

    public async Task<DepartmentManagerIssueDetailResponse> UpdateIssueStatusAsync(long issueId, DepartmentManagerUpdateIssueStatusRequest request, string userId, CancellationToken cancellationToken = default)
    {
        var issue = await _context.Issues.FindAsync(new object[] { issueId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay su co co ID = {issueId}.");

        var issueStatus = await _context.IssueStatuses.FindAsync(new object[] { request.StatusId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Khong tim thay trang thai co ID = {request.StatusId}.");

        if (string.Equals(issueStatus.StatusCode, "CLOSED", StringComparison.OrdinalIgnoreCase)
            && !issue.ResolvedAt.HasValue)
        {
            throw new InvalidOperationException("Sự cố phải chuyển sang RESOLVED trước khi CLOSED.");
        }

        var now = DateTime.UtcNow;
        var fromStatusId = issue.StatusId;

        _context.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = issueId,
            CreatedBy = userId,
            FromStatusId = fromStatusId,
            ToStatusId = request.StatusId,
            Note = request.Note,
            ProgressPercent = request.ProgressPercent,
            IsSystemGenerated = false,
            CreatedAt = now
        });

        issue.StatusId = request.StatusId;

        var resolvedStatus = await _context.IssueStatuses.FirstOrDefaultAsync(s => s.StatusCode == "RESOLVED", cancellationToken);
        if (resolvedStatus != null && request.StatusId == resolvedStatus.StatusId)
            issue.ResolvedAt = now;

        if (string.Equals(issueStatus.StatusCode, "CLOSED", StringComparison.OrdinalIgnoreCase))
            issue.ClosedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return (await GetIssueDetailAsync(issueId, cancellationToken))!;
    }

    private static DepartmentManagerIssueSummary MapToSummary(Issue i, int assignedMemberCount) => new()
    {
        IssueId = i.IssueId,
        PublicCode = i.PublicCode,
        Title = i.Title,
        IssueTypeId = i.IssueTypeId,
        IssueTypeName = i.IssueType?.TypeName ?? string.Empty,
        IssueTypeCode = i.IssueType?.TypeCode ?? string.Empty,
        PriorityId = i.PriorityId,
        PriorityName = i.Priority?.PriorityName ?? string.Empty,
        PriorityColor = GetPriorityColor(i.Priority?.SeverityRank ?? 0),
        StatusId = i.StatusId,
        StatusName = i.Status?.StatusName ?? string.Empty,
        ReportedAt = i.ReportedAt,
        SlaStatus = GetSlaStatus(i.Sla),
        FirstResponseDueAt = i.Sla?.FirstResponseDueAt,
        ResolutionDueAt = i.Sla?.ResolutionDueAt,
        ThumbnailUrl = i.Attachments
            .Where(a => a.Kind.ToUpperInvariant() == "IMAGE" && !string.IsNullOrWhiteSpace(a.ThumbnailUrl))
            .Select(a => a.ThumbnailUrl)
            .FirstOrDefault()
            ?? i.Attachments
                .Where(a => a.Kind.ToUpperInvariant() == "IMAGE")
                .Select(a => a.FileUrl)
                .FirstOrDefault(),
        Latitude = i.Latitude,
        Longitude = i.Longitude,
        IssueStatus = GetIssueStatus(i),
        AssignedMemberCount = assignedMemberCount
    };

    private static string GetPriorityColor(byte rank) => rank switch
    {
        1 => "#F44336",
        2 => "#FF9800",
        3 => "#FFC107",
        _ => "#4CAF50"
    };

    private static string GetSlaStatus(IssueSla? sla)
    {
        if (sla is null) return "NO_SLA";
        if (sla.ResolvedAt.HasValue) return "RESOLVED";
        var now = DateTime.UtcNow;
        if (sla.ResolutionDueAt < now) return "BREACHED";
        if (!sla.FirstRespondedAt.HasValue && sla.FirstResponseDueAt.HasValue && sla.FirstResponseDueAt < now) return "RESPONSE_BREACHED";
        if (sla.ResolutionDueAt < now.AddHours(2)) return "AT_RISK";
        return "ON_TRACK";
    }

    private static string GetIssueStatus(Issue issue)
    {
        if (issue.Status?.StatusCode?.ToUpperInvariant() == "RESOLVED") return "RESOLVED";
        if (issue.Status?.StatusCode?.ToUpperInvariant() == "CLOSED") return "CLOSED";
        if (issue.Sla != null && issue.Sla.ResolutionDueAt < DateTime.UtcNow) return "BREACHED";
        if (issue.Sla != null && !issue.Sla.FirstRespondedAt.HasValue && issue.Sla.FirstResponseDueAt.HasValue && issue.Sla.FirstResponseDueAt < DateTime.UtcNow) return "RESPONSE_BREACHED";
        if (issue.Sla != null && issue.Sla.ResolutionDueAt < DateTime.UtcNow.AddHours(2)) return "AT_RISK";
        return "ACTIVE";
    }
}
