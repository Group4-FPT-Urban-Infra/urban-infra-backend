using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffIncidentDetailResponse
{
    public long IssueId { get; set; }
    public long ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime ReportedAt { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
    public IReadOnlyList<IssueTimelineItemResponse> Timeline { get; set; } = [];
}