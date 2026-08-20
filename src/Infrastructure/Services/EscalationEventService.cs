using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.EscalationEvents;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class EscalationEventService : IEscalationEventService
{
    private readonly AppDbContext _db;

    public EscalationEventService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EscalationEventResponse>> GetByIssueAsync(long issueId, CancellationToken ct = default)
    {
        // Validate issue exists
        var issueExists = await _db.Issues.AnyAsync(i => i.IssueId == issueId, ct);
        if (!issueExists) return Array.Empty<EscalationEventResponse>();

        var items = await _db.EscalationEvents
            .AsNoTracking()
            .Where(e => e.IssueId == issueId)
            .OrderByDescending(e => e.TriggeredAt)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<EscalationEventResponse>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default)
    {
        var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentId == departmentId, ct);
        if (!deptExists) return Array.Empty<EscalationEventResponse>();

        var items = await _db.EscalationEvents
            .AsNoTracking()
            .Where(e => e.TargetDepartmentId == departmentId)
            .OrderByDescending(e => e.TriggeredAt)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<EscalationEventResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.EscalationEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e == null ? null : Map(e);
    }

    public async Task<EscalationEventResponse> AcknowledgeAsync(Guid id, string currentUserId, CancellationToken ct = default)
    {
        var e = await _db.EscalationEvents.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e == null) throw new KeyNotFoundException("EscalationEvent not found.");

        if (!string.IsNullOrWhiteSpace(e.AcknowledgedBy))
            throw new InvalidOperationException("EscalationEvent already acknowledged.");

        e.AcknowledgedBy = currentUserId;
        e.AcknowledgedAt = DateTime.UtcNow;
        e.EventStatus = "Acknowledged";
        e.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Map(e);
    }

    private static EscalationEventResponse Map(Domain.Entities.EscalationEvent x) => new()
    {
        Id = x.Id,
        IssueId = x.IssueId,
        EscalationRuleId = x.EscalationRuleId,
        TargetDepartmentId = x.TargetDepartmentId,
        TargetUserId = x.TargetUserId,
        TriggeredAt = x.TriggeredAt,
        AcknowledgedAt = x.AcknowledgedAt,
        AcknowledgedBy = x.AcknowledgedBy,
        EventStatus = x.EventStatus,
        Note = x.Note
    };
}
