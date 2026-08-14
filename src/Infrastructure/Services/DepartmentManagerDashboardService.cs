using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class DepartmentManagerDashboardService : IDepartmentManagerDashboardService
{
    private readonly AppDbContext _context;

    public DepartmentManagerDashboardService(AppDbContext context) => _context = context;

    public async Task<DepartmentManagerDashboardStatsResponse> GetStatsAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var currentMonth = new DateTime(now.Year, now.Month, 1);

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var assignedAssignmentIds = await _context.IssueAssignmentMembers
            .Where(m => currentAssignmentIds.Contains(m.AssignmentId) && m.Status != AssignmentMemberStatus.Rejected)
            .Select(m => m.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unassignedCount = currentAssignmentIds.Except(assignedAssignmentIds).Count();

        var processingCount = await _context.IssueAssignmentMembers
            .Where(m => currentAssignmentIds.Contains(m.AssignmentId) && m.Status == AssignmentMemberStatus.Accepted)
            .CountAsync(cancellationToken);

        var issueIds = await _context.IssueAssignments
            .Where(a => currentAssignmentIds.Contains(a.AssignmentId))
            .Select(a => a.IssueId)
            .ToListAsync(cancellationToken);

        var slaBreachRisks = await _context.IssueSlas
            .Where(s => issueIds.Contains(s.IssueId) && s.ResolutionDueAt < now.AddHours(2) && !s.ResolvedAt.HasValue)
            .CountAsync(cancellationToken);

        return new DepartmentManagerDashboardStatsResponse
        {
            UnassignedCount = unassignedCount,
            ProcessingCount = processingCount,
            AvgResponseTimeMinutes = 42,
            SlaBreachRisksCount = slaBreachRisks,
            UnassignedTrend = 0,
            ProcessingTrend = 0
        };
    }

    public async Task<TeamWorkloadResponse> GetTeamWorkloadAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var memberUserIds = await _context.DepartmentMembers
            .Where(dm => dm.DepartmentId == departmentId && dm.IsActive)
            .Select(dm => dm.UserId)
            .ToListAsync(cancellationToken);

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var members = await _context.Users
            .Where(u => memberUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var memberAssignments = await _context.IssueAssignmentMembers
            .Where(m => currentAssignmentIds.Contains(m.AssignmentId))
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, Assigned = g.Count(), Resolved = g.Count(m => m.Status == AssignmentMemberStatus.Completed) })
            .ToListAsync(cancellationToken);

        var result = members.Select(u =>
        {
            var stats = memberAssignments.FirstOrDefault(x => x.UserId == u.Id);
            return new TeamWorkloadItem
            {
                UserId = u.Id,
                FullName = u.FullName,
                AvatarUrl = null,
                AssignedCount = stats?.Assigned ?? 0,
                ResolvedCount = stats?.Resolved ?? 0
            };
        }).ToList();

        return new TeamWorkloadResponse { Members = result };
    }

    public async Task<IReadOnlyList<DepartmentManagerIssueSummary>> GetUnassignedIssuesAsync(int departmentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var assignedAssignmentIds = await _context.IssueAssignmentMembers
            .Where(m => currentAssignmentIds.Contains(m.AssignmentId) && m.Status != AssignmentMemberStatus.Rejected)
            .Select(m => m.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unassignedAssignmentIds = currentAssignmentIds.Except(assignedAssignmentIds).ToList();

        var issues = await _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Sla)
            .Where(i => _context.IssueAssignments
                .Where(a => unassignedAssignmentIds.Contains(a.AssignmentId))
                .Select(a => a.IssueId)
                .Contains(i.IssueId))
            .OrderByDescending(i => i.PriorityId)
            .ThenBy(i => i.ReportedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return issues.Select(i => new DepartmentManagerIssueSummary
        {
            IssueId = i.IssueId,
            PublicCode = i.PublicCode,
            Title = i.Title,
            IssueTypeName = i.IssueType?.TypeName ?? string.Empty,
            PriorityName = i.Priority?.PriorityName ?? string.Empty,
            PriorityColor = GetPriorityColor(i.Priority?.SeverityRank ?? 0),
            StatusName = i.Status?.StatusName ?? string.Empty,
            ReportedAt = i.ReportedAt,
            SlaStatus = GetSlaStatus(i.Sla),
            FirstResponseDueAt = i.Sla?.FirstResponseDueAt,
            ResolutionDueAt = i.Sla?.ResolutionDueAt
        }).ToList();
    }

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
}
