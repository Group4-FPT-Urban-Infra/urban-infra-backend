using UrbanInfraSystem.Application.DTOs.Issues; 


namespace UrbanInfraSystem.Application.DTOs.Staff;

public class GetMyTasksRequest : PagedRequest
{
    public string? Status { get; set; }
    public int? Priority { get; set; }
}