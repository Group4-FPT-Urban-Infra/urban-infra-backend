using System;
using UrbanInfraSystem.Domain.Common;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Runtime record when an escalation rule is triggered for an issue's SLA.
/// </summary>
public class EscalationEvent : BaseEntity
{
    // Reference to Issue (IssueId long) because IssueSla entity not present in this project.
    public long IssueId { get; set; }

    // The rule that caused this event (optional)
    public Guid? EscalationRuleId { get; set; }

    public int? TargetDepartmentId { get; set; }

    // Actual user targeted (ApplicationUser.Id is string)
    public string? TargetUserId { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcknowledgedAt { get; set; }

    public string? AcknowledgedBy { get; set; }

    // Event status: e.g., "Pending", "Acknowledged" — stored as string to be flexible
    public string? EventStatus { get; set; }

    public string? Note { get; set; }

    // Navigation
    public Issue? Issue { get; set; }
    public EscalationRule? EscalationRule { get; set; }
    public Department? TargetDepartment { get; set; }
}
