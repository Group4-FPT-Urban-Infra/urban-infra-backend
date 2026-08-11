namespace UrbanInfraSystem.Domain.Entities;

public class RoutingRule
{
    public int RoutingRuleId { get; set; }
    public int IssueTypeId { get; set; }
    public int AreaId { get; set; }
    public int DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public IssueType IssueType { get; set; } = default!;
    public Area Area { get; set; } = default!;
    public Department Department { get; set; } = default!;
}
