using System;
using UrbanInfraSystem.Domain.Common;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Escalation rule linking a SLA policy and an overdue threshold to a target department or role.
/// </summary>
public class EscalationRule : BaseEntity
{
    public Guid SlaPolicyId { get; set; }

    public int OverdueMinutes { get; set; }

    public int? TargetDepartmentId { get; set; }

    /// <summary>
    /// Role name (as in Identity role Name), e.g. "DepartmentHead" or "Admin".
    /// Stored as string to avoid coupling to Identity role id values.
    /// </summary>
    public string? TargetRoleName { get; set; }

    // Escalation level (1 = first level, 2 = second level, ...)
    public int EscalationLevel { get; set; } = 1;

    // Notification configuration (optional)
    public string? NotificationTitle { get; set; }
    public string? NotificationTemplate { get; set; }

    // Is the rule active/enabled
    public bool IsActive { get; set; } = true;

    // Navigation
    public SlaPolicy? SlaPolicy { get; set; }
    public Department? TargetDepartment { get; set; }
}
