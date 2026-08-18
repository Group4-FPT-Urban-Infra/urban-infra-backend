using System;
using System.Collections.Generic;

namespace UrbanInfraSystem.Infrastructure.TempCheck;

public partial class Department
{
    public int DepartmentId { get; set; }

    public int? ParentDepartmentId { get; set; }

    public string DepartmentCode { get; set; } = null!;

    public string DepartmentName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<DepartmentMember> DepartmentMembers { get; set; } = new List<DepartmentMember>();

    public virtual ICollection<EscalationEvent> EscalationEvents { get; set; } = new List<EscalationEvent>();

    public virtual ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();

    public virtual ICollection<Department> InverseParentDepartment { get; set; } = new List<Department>();

    public virtual Department? ParentDepartment { get; set; }

    public virtual ICollection<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();
}
