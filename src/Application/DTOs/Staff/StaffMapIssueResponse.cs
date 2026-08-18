namespace UrbanInfraSystem.Application.DTOs.Staff;

public class StaffMapIssueResponse
{
    public long IssueId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
}