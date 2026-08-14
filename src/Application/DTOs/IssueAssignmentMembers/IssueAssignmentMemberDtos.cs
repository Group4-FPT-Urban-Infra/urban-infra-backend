using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.IssueAssignmentMembers;

public class AssignMemberRequest
{
    [Required]
    public string UserId { get; set; } = default!;

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class UpdateMemberStatusRequest
{
    [Required]
    public string Status { get; set; } = default!;

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public class IssueAssignmentMemberResponse
{
    public long MemberId { get; set; }
    public long AssignmentId { get; set; }
    public string UserId { get; set; } = default!;
    public string UserFullName { get; set; } = default!;
    public string UserEmail { get; set; } = default!;
    public string AssignedBy { get; set; } = default!;
    public string AssignorFullName { get; set; } = default!;
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = default!;
    public string? Note { get; set; }
}
