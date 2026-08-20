using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffIncidentDetailResponse
{
    public long IssueId { get; set; }
    public long ReportId { get; set; }
    public string PublicCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public LookupItemResponse Status { get; set; } = default!;
    public LookupItemResponse Priority { get; set; } = default!;
    public LookupItemResponse IssueType { get; set; } = default!;
    public LookupItemResponse Area { get; set; } = default!;
    public DateTime ReportedAt { get; set; }
    public bool IsSlaBreached { get; set; }

    // Reporter info
    public ReporterInfoResponse Reporter { get; set; } = default!;

    // Current staff member info (the logged-in staff)
    public CurrentMemberInfo? CurrentMember { get; set; }

    // Assignment info
    public AssignmentInfoResponse? Assignment { get; set; }

    // Separate images
    public IReadOnlyList<IssueAttachmentResponse> ReporterImages { get; set; } = [];
    public IReadOnlyList<IssueAttachmentResponse> StaffImages { get; set; } = [];

    // Timeline
    public IReadOnlyList<IssueTimelineItemResponse> Timeline { get; set; } = [];
}

public class ReporterInfoResponse
{
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class AssignmentInfoResponse
{
    public LookupItemResponse Department { get; set; } = default!;
    public string? AssigneeName { get; set; }
    public string? AssignmentStatus { get; set; }
}

public class CurrentMemberInfo
{
    public long MemberId { get; set; }
    public string Status { get; set; } = string.Empty;
}