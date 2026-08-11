using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.RoutingRules;

public class CreateRoutingRuleRequest
{
    [Range(1, int.MaxValue)]
    public int IssueTypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int AreaId { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateRoutingRuleRequest : CreateRoutingRuleRequest;

public class RoutingRuleFilterRequest
{
    public int? IssueTypeId { get; set; }
    public int? AreaId { get; set; }
    public int? DepartmentId { get; set; }
    public bool IncludeInactive { get; set; }
}

public class RoutingRuleResponse
{
    public int RoutingRuleId { get; set; }
    public int IssueTypeId { get; set; }
    public string IssueTypeCode { get; set; } = default!;
    public string IssueTypeName { get; set; } = default!;
    public int AreaId { get; set; }
    public string AreaCode { get; set; } = default!;
    public string AreaName { get; set; } = default!;
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = default!;
    public string DepartmentName { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
