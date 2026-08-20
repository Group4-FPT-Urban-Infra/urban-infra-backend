using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services.Elaboration;

/// <summary>
/// Business logic for detecting SLA breaches and creating EscalationEvents.
/// Separated from the background worker to allow manual invocation in tests.
/// </summary>
public class EscalationProcessor
{
    private readonly AppDbContext _db;
    private readonly ILogger<EscalationProcessor> _logger;

    public EscalationProcessor(AppDbContext db, ILogger<EscalationProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Run one pass of detection: find overdue IssueSlas and create EscalationEvents for matching rules.
    /// Idempotent: will not create duplicate events for same IssueId + EscalationRuleId.
    /// </summary>
    public async Task ProcessOnceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SLA breach processor started.");

        var now = DateTime.UtcNow;

        // Fetch overdue IssueSlas that haven't been resolved yet
        var overdueSlas = await _db.IssueSlas
            .AsNoTracking()
            .Where(s => s.ResolvedAt == null && s.ResolutionDueAt < now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} overdue IssueSlas.", overdueSlas.Count);

        int created = 0, skipped = 0;

        foreach (var sla in overdueSlas)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var overdueMinutes = (int)Math.Floor((now - sla.ResolutionDueAt).TotalMinutes);

            // Only check ResponseOverdue rules here (based on ResolutionDueAt).
            // FirstResponseOverdue and ApproachResponseDeadline are handled by SlaCheckBackgroundService.
            var rules = await _db.EscalationRules
                .AsNoTracking()
                .Where(r => r.SlaPolicyId == sla.SlaPolicyId
                         && r.IsActive
                         && r.EscalationType == Domain.Enums.EscalationType.ResponseOverdue)
                .OrderBy(r => r.EscalationLevel)
                .ToListAsync(cancellationToken);

            foreach (var rule in rules)
            {
                if (overdueMinutes < rule.OverdueMinutes) continue;

                // check event exists for IssueId + EscalationRuleId
                var exists = await _db.EscalationEvents
                    .AnyAsync(e => e.IssueId == sla.IssueId && e.EscalationRuleId == rule.Id && !e.IsDeleted, cancellationToken);

                if (exists)
                {
                    skipped++;
                    continue;
                }

                var ev = new Domain.Entities.EscalationEvent
                {
                    IssueId = sla.IssueId,
                    EscalationRuleId = rule.Id,
                    TargetDepartmentId = rule.TargetDepartmentId,
                    // TargetUserId resolution not implemented here (requires business logic)
                    TargetUserId = null,
                    TriggeredAt = now,
                    EventStatus = "Pending",
                    Note = null,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.EscalationEvents.Add(ev);
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    created++;
                    _logger.LogInformation(
                        "[EscalationProcessor] Created EscalationEvent: IssueId={IssueId}, RuleId={RuleId}, Type={EscalationType}, Level={Level}, TargetDept={TargetDeptId}",
                        sla.IssueId, rule.Id, rule.EscalationType, rule.EscalationLevel, rule.TargetDepartmentId);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Failed to insert EscalationEvent for Issue {IssueId} Rule {RuleId}; possibly concurrent insert.", sla.IssueId, rule.Id);
                    skipped++;
                }
            }
        }

        _logger.LogInformation("SLA breach processor completed. Created: {Created}, Skipped: {Skipped}", created, skipped);
    }
}
