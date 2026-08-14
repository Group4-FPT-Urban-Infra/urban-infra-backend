namespace UrbanInfraSystem.Application.DTOs.DepartmentManager;

public class StaffMemberResponse
{
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public int AssignedCount { get; set; }
    public int ResolvedCount { get; set; }
    public int PendingCount { get; set; }
    public double ResolutionRate { get; set; }
    public double AvgResolutionHours { get; set; }
    public int CurrentWorkload { get; set; }
}

public class StaffDetailResponse : StaffMemberResponse
{
    public List<StaffAssignmentItem> RecentAssignments { get; set; } = new();
}

public class StaffAssignmentItem
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
