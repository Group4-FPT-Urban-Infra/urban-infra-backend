namespace UrbanInfraSystem.Domain.Entities;

public class DepartmentMember
{
    public int DepartmentId { get; set; }
    public string UserId { get; set; } = default!;
    public string? JobTitle { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Department Department { get; set; } = default!;
}
