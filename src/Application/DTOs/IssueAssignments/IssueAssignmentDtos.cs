using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.IssueAssignments;

public class ManualRouteRequest
{
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class ReassignIssueRequest
{
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class IssueAssignmentResponse
{
    public long AssignmentId { get; set; }
    public long IssueId { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = default!;
    public string DepartmentName { get; set; } = default!;
    public int? RoutingRuleId { get; set; }
    public string AssignmentMethod { get; set; } = default!;
    public string? AssignmentNote { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsCurrent { get; set; }
}
