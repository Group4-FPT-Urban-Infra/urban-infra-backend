using System;

namespace UrbanInfraSystem.Application.DTOs.ReRouteRequests;

public class ReRouteRequestDto
{
    public long Id { get; set; }
    public long IssueId { get; set; }
    public int CurrentDepartmentId { get; set; }
    public string CurrentDepartmentName { get; set; } = default!;
    public int TargetDepartmentId { get; set; }
    public string TargetDepartmentName { get; set; } = default!;
    public string? Note { get; set; }
    public string Status { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
}
