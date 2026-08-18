using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class RoutingRule
{
    public int RoutingRuleId { get; set; }

    public int IssueTypeId { get; set; }

    public int AreaId { get; set; }

    public int DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Area Area { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual IssueType IssueType { get; set; } = null!;
}
