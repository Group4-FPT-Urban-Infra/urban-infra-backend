namespace UrbanInfraSystem.Application.DTOs.Staff;

public class GetMapIssuesRequest
{
    public double NorthEastLat { get; set; }
    public double NorthEastLng { get; set; }
    public double SouthWestLat { get; set; }
    public double SouthWestLng { get; set; }
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public bool? AssignedToMe { get; set; }
}