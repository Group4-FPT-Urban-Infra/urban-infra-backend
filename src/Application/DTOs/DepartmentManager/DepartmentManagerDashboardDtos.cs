namespace UrbanInfraSystem.Application.DTOs.DepartmentManager;

public class DepartmentManagerDashboardStatsResponse
{
    public int UnassignedCount { get; set; }
    public int ProcessingCount { get; set; }
    public double AvgResponseTimeMinutes { get; set; }
    public int SlaBreachRisksCount { get; set; }
    public int UnassignedTrend { get; set; }
    public int ProcessingTrend { get; set; }
}

public class TeamWorkloadItem
{
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public int AssignedCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class TeamWorkloadResponse
{
    public List<TeamWorkloadItem> Members { get; set; } = new();
}

public class DepartmentManagerIssueSummary
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int IssueTypeId { get; set; }
    public string IssueTypeName { get; set; } = default!;
    public string IssueTypeCode { get; set; } = default!;
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = default!;
    public string PriorityColor { get; set; } = default!;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = default!;
    public DateTime ReportedAt { get; set; }
    public string? SlaStatus { get; set; }
    public DateTime? FirstResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public string? ThumbnailUrl { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
