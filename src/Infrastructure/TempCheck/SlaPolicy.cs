using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class SlaPolicy
{
    public Guid Id { get; set; }

    public int IssueTypeId { get; set; }

    public int PriorityId { get; set; }

    public int ResolutionMinutes { get; set; }

    public int FirstResponseMinutes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();

    public virtual IssueType IssueType { get; set; } = null!;

    public virtual IssuePriority Priority { get; set; } = null!;
}
