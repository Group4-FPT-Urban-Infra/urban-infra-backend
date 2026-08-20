using UrbanInfraSystem.Application.DTOs.Issues;


namespace UrbanInfraSystem.Application.DTOs.Staff;

public class SearchStaffIncidentsRequest : PagedRequest
{
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public string? Query { get; set; }
}