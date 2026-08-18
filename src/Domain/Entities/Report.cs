namespace UrbanInfraSystem.Domain.Entities;

/// <summary>Một lần công dân gửi phản ánh; có thể sinh ra nhiều Issue để xử lý độc lập.</summary>
public class Report
{
    public long ReportId { get; set; }
    public string ReporterId { get; set; } = default!;
    public int AreaId { get; set; }
    public string PublicCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? AddressText { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Area Area { get; set; } = default!;
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<ReportIssueType> ReportIssueTypes { get; set; } = new List<ReportIssueType>();
}
