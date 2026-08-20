using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class DepartmentManagerSlaService : IDepartmentManagerSlaService
{
    private readonly AppDbContext _context;

    public DepartmentManagerSlaService(AppDbContext context) => _context = context;

    public async Task<SlaOverviewResponse> GetOverviewAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.AssignedAt < monthEnd)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var issueIds = await _context.IssueAssignments
            .Where(a => currentAssignmentIds.Contains(a.AssignmentId))
            .Select(a => a.IssueId)
            .ToListAsync(cancellationToken);

        var slas = await _context.IssueSlas
            .Where(s => issueIds.Contains(s.IssueId) && s.CreatedAt >= monthStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalIssuesInMonth = slas.Count;
        var resolvedOnTime = slas.Count(s => s.ResolvedAt.HasValue && s.ResolutionDueAt < now && s.ResolvedAt <= s.ResolutionDueAt);
        var breached = slas.Count(s => s.ResolvedAt.HasValue && s.ResolvedAt > s.ResolutionDueAt)
                     + slas.Count(s => !s.ResolvedAt.HasValue && s.ResolutionDueAt < now);

        return new SlaOverviewResponse
        {
            TotalIssuesInMonth = totalIssuesInMonth,
            ResolvedOnTime = resolvedOnTime,
            Breached = breached,
            ResolutionRate = totalIssuesInMonth > 0 ? Math.Round((double)resolvedOnTime / totalIssuesInMonth * 100, 1) : 0,
            AvgResponseTimeMinutes = 45,
            AvgResolutionTimeMinutes = 180
        };
    }

    public async Task<IReadOnlyList<SlaNearDeadlineItem>> GetNearDeadlineAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var twoHoursLater = now.AddHours(2);

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsCurrent)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var issueIds = await _context.IssueAssignments
            .Where(a => currentAssignmentIds.Contains(a.AssignmentId))
            .Select(a => a.IssueId)
            .ToListAsync(cancellationToken);

        var nearDeadlineSlas = await _context.IssueSlas
            .Include(s => s.Issue)
                .ThenInclude(i => i.Priority)
            .Include(s => s.Issue)
                .ThenInclude(i => i.Status)
            .Where(s => issueIds.Contains(s.IssueId) &&
                        !s.ResolvedAt.HasValue &&
                        ((s.FirstResponseDueAt.HasValue && s.FirstResponseDueAt <= twoHoursLater && !s.FirstRespondedAt.HasValue) ||
                         (s.ResolutionDueAt <= twoHoursLater)))
            .OrderBy(s => s.ResolutionDueAt)
            .Take(20)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return nearDeadlineSlas.Select(sla =>
        {
            var isFirstResponse = !sla.FirstRespondedAt.HasValue && sla.FirstResponseDueAt.HasValue && sla.FirstResponseDueAt <= twoHoursLater;
            var dueAt = isFirstResponse ? sla.FirstResponseDueAt!.Value : sla.ResolutionDueAt;
            var minutesRemaining = (int)(dueAt - now).TotalMinutes;

            return new SlaNearDeadlineItem
            {
                IssueId = sla.IssueId,
                PublicCode = sla.Issue?.PublicCode ?? string.Empty,
                Title = sla.Issue?.Title ?? string.Empty,
                DeadlineType = isFirstResponse ? "First Response" : "Resolution",
                DueAt = dueAt,
                MinutesRemaining = minutesRemaining,
                PriorityName = sla.Issue?.Priority?.PriorityName ?? string.Empty,
                StatusName = sla.Issue?.Status?.StatusName ?? string.Empty
            };
        }).ToList();
    }

    public async Task<SlaHistoryResponse> GetHistoryAsync(int departmentId, int months = 6, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startDate = now.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        var currentAssignmentIds = await _context.IssueAssignments
            .Where(a => a.DepartmentId == departmentId && a.AssignedAt >= startDate)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var issueIds = await _context.IssueAssignments
            .Where(a => currentAssignmentIds.Contains(a.AssignmentId))
            .Select(a => a.IssueId)
            .ToListAsync(cancellationToken);

        var slas = await _context.IssueSlas
            .Where(s => issueIds.Contains(s.IssueId) && s.CreatedAt >= startDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new List<SlaHistoryItem>();
        for (int i = 0; i < months; i++)
        {
            var monthDate = now.AddMonths(-i);
            var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var monthSlas = slas.Where(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd).ToList();
            var totalIssues = monthSlas.Count;
            var resolvedOnTime = monthSlas.Count(s => s.ResolvedAt.HasValue && s.ResolvedAt <= s.ResolutionDueAt);
            var breached = monthSlas.Count(s => s.ResolvedAt.HasValue && s.ResolvedAt > s.ResolutionDueAt)
                         + monthSlas.Count(s => !s.ResolvedAt.HasValue && s.ResolutionDueAt < monthEnd);

            result.Add(new SlaHistoryItem
            {
                Year = monthDate.Year,
                Month = monthDate.Month,
                MonthName = monthDate.ToString("MMMM"),
                TotalIssues = totalIssues,
                ResolvedOnTime = resolvedOnTime,
                Breached = breached,
                ResolutionRate = totalIssues > 0 ? Math.Round((double)resolvedOnTime / totalIssues * 100, 1) : 0
            });
        }

        return new SlaHistoryResponse
        {
            Items = result,
            TotalCount = result.Count
        };
    }
}
