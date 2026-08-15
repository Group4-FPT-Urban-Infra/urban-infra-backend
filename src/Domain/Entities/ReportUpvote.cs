namespace UrbanInfraSystem.Domain.Entities;

public class ReportUpvote
{
    public long ReportId { get; set; }
    public string UserId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Report Report { get; set; } = default!;
}
