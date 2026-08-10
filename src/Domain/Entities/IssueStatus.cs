namespace UrbanInfraSystem.Domain.Entities;

public class IssueStatus
{
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = default!;
    public string StatusName { get; set; } = default!;
    public bool IsClosed { get; set; }
    public bool IsPublicVisible { get; set; } = true;
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
