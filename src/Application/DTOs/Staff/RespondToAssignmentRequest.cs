namespace UrbanInfraSystem.Application.DTOs.Staff;

public class RespondToAssignmentRequest
{
    public bool Accepted { get; set; }
    public string? RejectionReason { get; set; }
}