namespace UrbanInfraSystem.Domain.Entities;

public class IssuePriority
{
    public int PriorityId { get; set; }
    public string PriorityCode { get; set; } = default!;
    public string PriorityName { get; set; } = default!;
    public byte SeverityRank { get; set; }
    public bool IsActive { get; set; } = true;
}
