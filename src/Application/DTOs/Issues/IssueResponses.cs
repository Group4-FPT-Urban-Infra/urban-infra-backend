namespace UrbanInfraSystem.Application.DTOs.Issues;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class LookupItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
}

public class IssueSummaryResponse
{
    public long Id { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public LookupItemResponse IssueType { get; set; } = default!;
    public LookupItemResponse Area { get; set; } = default!;
    public LookupItemResponse Priority { get; set; } = default!;
    public LookupItemResponse Status { get; set; } = default!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int UpvoteCount { get; set; }
    public bool HasUpvoted { get; set; }
    public DateTime ReportedAt { get; set; }
}

public class IssueDetailResponse : IssueSummaryResponse
{
    public string Description { get; set; } = default!;
    public string? AddressText { get; set; }
    public string ReporterDisplayName { get; set; } = default!;
    public LookupItemResponse? CurrentDepartment { get; set; }
    public DuplicateIssueResponse? DuplicateOf { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IssueSlaResponse? Sla { get; set; }
    public IReadOnlyList<IssueAttachmentResponse> Attachments { get; set; } = [];
}

public class IssueSlaResponse
{
    public long Id { get; set; }
    public long IssueId { get; set; }
    public Guid? SlaPolicyId { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public DateTime? FirstResponseDueAt { get; set; }
    public DateTime ResolutionDueAt { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsFirstResponseBreached { get; set; }
    public bool IsResolutionBreached { get; set; }
}

public class DuplicateIssueResponse
{
    public long Id { get; set; }
    public string PublicCode { get; set; } = default!;
}

public class NearbyIssueResponse : IssueSummaryResponse
{
    public double DistanceMeters { get; set; }
}

public class IssueAttachmentResponse
{
    public long Id { get; set; }
    public string Kind { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string? ThumbnailUrl { get; set; }
    public string MimeType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class IssueTimelineItemResponse
{
    public long Id { get; set; }
    public string UpdateType { get; set; } = default!;
    public LookupItemResponse? FromStatus { get; set; }
    public LookupItemResponse? ToStatus { get; set; }
    public byte? ProgressPercent { get; set; }
    public string? Note { get; set; }
    public bool IsSystemGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<IssueAttachmentResponse> Attachments { get; set; } = [];
}

public class UpvoteResponse
{
    public long IssueId { get; set; }
    public bool HasUpvoted { get; set; }
    public int UpvoteCount { get; set; }
}
