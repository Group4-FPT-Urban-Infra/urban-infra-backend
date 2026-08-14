namespace UrbanInfraSystem.Application.DTOs.Dashboard;

public class AuditLogResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = default!;
    public string Subtitle { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Type of the log (e.g., "error", "info", "warning", "success")
    /// </summary>
    public string Type { get; set; } = default!;
}
