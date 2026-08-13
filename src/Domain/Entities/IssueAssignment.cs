namespace UrbanInfraSystem.Domain.Entities;

public class IssueAssignment
{
    public long AssignmentId { get; set; }
    public long IssueId { get; set; }
    public int DepartmentId { get; set; }
    public int? RoutingRuleId { get; set; }
    public string AssignmentMethod { get; set; } = default!;
    public string? AssignmentNote { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsCurrent { get; set; } = true;

    public Issue Issue { get; set; } = default!;
    public Department Department { get; set; } = default!;
    public RoutingRule? RoutingRule { get; set; }
}
