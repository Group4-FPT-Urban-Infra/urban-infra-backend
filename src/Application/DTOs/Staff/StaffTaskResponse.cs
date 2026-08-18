namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffTaskResponse
{
    public long IssueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime AssignedAt { get; set; }
}