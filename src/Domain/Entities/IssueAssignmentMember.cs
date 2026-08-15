namespace UrbanInfraSystem.Domain.Entities;

public class IssueAssignmentMember
{
    public long MemberId { get; set; }
    public long AssignmentId { get; set; }
    public string UserId { get; set; } = default!;
    public string AssignedBy { get; set; } = default!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = AssignmentMemberStatus.Pending;
    public string? Note { get; set; }

    public IssueAssignment Assignment { get; set; } = default!;
}

public static class AssignmentMemberStatus
{
    public const string Pending = "PENDING";
    public const string Accepted = "ACCEPTED";
    public const string Rejected = "REJECTED";
    public const string Completed = "COMPLETED";
}
