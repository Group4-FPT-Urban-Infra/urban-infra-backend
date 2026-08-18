using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.DTOs.Reports;

public class ReportSummaryResponse
{
    public long Id { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public LookupItemResponse Area { get; set; } = default!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime ReportedAt { get; set; }
    public int IssueCount { get; set; }
    public IReadOnlyList<LookupItemResponse> IssueTypes { get; set; } = [];
    public int ResolvedIssueCount { get; set; }
}

public class ReportDetailResponse : ReportSummaryResponse
{
    public string Description { get; set; } = default!;
    public string? AddressText { get; set; }
    public string ReporterDisplayName { get; set; } = default!;
    public IReadOnlyList<IssueAttachmentResponse> Attachments { get; set; } = [];
    public IReadOnlyList<ReportIssueResponse> Issues { get; set; } = [];
}

public class ReportIssueResponse
{
    public long Id { get; set; }
    public string PublicCode { get; set; } = default!;
    public LookupItemResponse IssueType { get; set; } = default!;
    public LookupItemResponse Priority { get; set; } = default!;
    public LookupItemResponse Status { get; set; } = default!;
    public LookupItemResponse? CurrentDepartment { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ReportUpdateResponse : IssueTimelineItemResponse
{
    public long ReportId { get; set; }
    public string ReportPublicCode { get; set; } = default!;
    public string ReportTitle { get; set; } = default!;
    public long IssueId { get; set; }
    public string IssuePublicCode { get; set; } = default!;
}
