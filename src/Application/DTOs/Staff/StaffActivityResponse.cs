namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffActivityResponse
{
    public long IssueId { get; set; }
    public string IssueTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public DateTime CreatedAt { get; set; }
}