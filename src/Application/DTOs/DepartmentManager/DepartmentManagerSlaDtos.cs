namespace UrbanInfraSystem.Application.DTOs.DepartmentManager;

public class SlaOverviewResponse
{
    public int TotalIssuesInMonth { get; set; }
    public int ResolvedOnTime { get; set; }
    public int Breached { get; set; }
    public double ResolutionRate { get; set; }
    public double AvgResponseTimeMinutes { get; set; }
    public double AvgResolutionTimeMinutes { get; set; }
}

public class SlaNearDeadlineItem
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string DeadlineType { get; set; } = default!;
    public DateTime DueAt { get; set; }
    public int MinutesRemaining { get; set; }
    public string PriorityName { get; set; } = default!;
    public string StatusName { get; set; } = default!;
}

public class SlaHistoryItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = default!;
    public int TotalIssues { get; set; }
    public int ResolvedOnTime { get; set; }
    public int Breached { get; set; }
    public double ResolutionRate { get; set; }
}

public class SlaHistoryResponse
{
    public List<SlaHistoryItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
