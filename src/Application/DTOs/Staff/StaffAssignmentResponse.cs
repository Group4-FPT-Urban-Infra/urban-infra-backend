namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffAssignmentResponse
{
    public long AssignmentId { get; set; }
    public long IssueId { get; set; }
    public string IssueTitle { get; set; } = string.Empty;
    public string? AssignedByManagerName { get; set; }
    public DateTime AssignedAt { get; set; }
}