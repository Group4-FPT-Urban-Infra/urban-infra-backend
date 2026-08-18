using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class EscalationEvent
{
    public Guid Id { get; set; }

    public long IssueId { get; set; }

    public Guid? EscalationRuleId { get; set; }

    public int? TargetDepartmentId { get; set; }

    public string? TargetUserId { get; set; }

    public DateTime TriggeredAt { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public string? AcknowledgedBy { get; set; }

    public string? EventStatus { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual EscalationRule? EscalationRule { get; set; }

    public virtual Issue Issue { get; set; } = null!;

    public virtual Department? TargetDepartment { get; set; }
}
