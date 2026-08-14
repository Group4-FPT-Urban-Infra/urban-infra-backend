using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class DepartmentManagerStaffService : IDepartmentManagerStaffService
{
    private readonly AppDbContext _context;

    public DepartmentManagerStaffService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<StaffMemberResponse>> GetStaffsAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var memberUserIds = await _context.DepartmentMembers
            .Where(dm => dm.DepartmentId == departmentId && dm.IsActive)
            .Select(dm => dm.UserId)
            .ToListAsync(cancellationToken);

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var memberStats = await _context.IssueAssignmentMembers
            .Where(m => currentAssignmentIds.Contains(m.AssignmentId))
            .GroupBy(m => m.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Assigned = g.Count(),
                Resolved = g.Count(m => m.Status == AssignmentMemberStatus.Completed),
                Pending = g.Count(m => m.Status == AssignmentMemberStatus.Pending || m.Status == AssignmentMemberStatus.Accepted)
            })
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .Where(u => memberUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.Select(u =>
        {
            var stats = memberStats.FirstOrDefault(s => s.UserId == u.Id);
            return new StaffMemberResponse
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                AvatarUrl = null,
                IsActive = u.IsActive,
                JoinedAt = u.CreatedAtUtc,
                AssignedCount = stats?.Assigned ?? 0,
                ResolvedCount = stats?.Resolved ?? 0,
                PendingCount = stats?.Pending ?? 0,
                ResolutionRate = stats?.Assigned > 0 ? Math.Round((double)(stats.Resolved) / stats.Assigned * 100, 1) : 0,
                AvgResolutionHours = 0,
                CurrentWorkload = stats?.Pending ?? 0
            };
        }).ToList();
    }

    public async Task<StaffDetailResponse?> GetStaffDetailAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null) return null;

        var department = await _context.DepartmentMembers
            .Include(dm => dm.Department)
            .FirstOrDefaultAsync(dm => dm.UserId == userId && dm.IsActive, cancellationToken);

        if (department is null) return null;

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == department.DepartmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var memberAssignments = await _context.IssueAssignmentMembers
            .Where(m => m.UserId == userId && currentAssignmentIds.Contains(m.AssignmentId))
            .OrderByDescending(m => m.AssignedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var assignmentIssueIds = await _context.IssueAssignments
            .Where(a => memberAssignments.Select(m => m.AssignmentId).Contains(a.AssignmentId))
            .Select(a => new { a.AssignmentId, a.IssueId })
            .ToListAsync(cancellationToken);

        var issueIds = assignmentIssueIds.Select(i => i.IssueId).Distinct().ToList();
        var issueDetails = await _context.Issues
            .Where(i => issueIds.Contains(i.IssueId))
            .ToDictionaryAsync(i => i.IssueId, cancellationToken);

        var totalAssigned = memberAssignments.Count;
        var totalResolved = memberAssignments.Count(m => m.Status == AssignmentMemberStatus.Completed);

        return new StaffDetailResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AvatarUrl = null,
            IsActive = user.IsActive,
            JoinedAt = user.CreatedAtUtc,
            AssignedCount = totalAssigned,
            ResolvedCount = totalResolved,
            PendingCount = totalAssigned - totalResolved,
            ResolutionRate = totalAssigned > 0 ? Math.Round((double)totalResolved / totalAssigned * 100, 1) : 0,
            AvgResolutionHours = 0,
            CurrentWorkload = totalAssigned - totalResolved,
            RecentAssignments = memberAssignments.Select(m =>
            {
                var issueId = assignmentIssueIds.FirstOrDefault(i => i.AssignmentId == m.AssignmentId)?.IssueId ?? 0;
                issueDetails.TryGetValue(issueId, out var issue);
                return new StaffAssignmentItem
                {
                    IssueId = issueId,
                    PublicCode = issue?.PublicCode ?? string.Empty,
                    Title = issue?.Title ?? string.Empty,
                    Status = m.Status,
                    AssignedAt = m.AssignedAt,
                    CompletedAt = m.EndedAt
                };
            }).ToList()
        };
    }
}
