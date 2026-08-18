using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class IssueType
{
    public int IssueTypeId { get; set; }

    public int? ParentIssueTypeId { get; set; }

    public string TypeCode { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public string? IconUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<IssueType> InverseParentIssueType { get; set; } = new List<IssueType>();

    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();

    public virtual IssueType? ParentIssueType { get; set; }

    public virtual ICollection<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();

    public virtual ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
}
