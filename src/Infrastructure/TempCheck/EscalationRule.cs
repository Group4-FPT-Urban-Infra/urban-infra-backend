using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class EscalationRule
{
    public Guid Id { get; set; }

    public Guid SlaPolicyId { get; set; }

    public int OverdueMinutes { get; set; }

    public int? TargetDepartmentId { get; set; }

    public string? TargetRoleName { get; set; }

    public int EscalationLevel { get; set; }

    public string? NotificationTitle { get; set; }

    public string? NotificationTemplate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<EscalationEvent> EscalationEvents { get; set; } = new List<EscalationEvent>();

    public virtual SlaPolicy SlaPolicy { get; set; } = null!;

    public virtual Department? TargetDepartment { get; set; }
}
