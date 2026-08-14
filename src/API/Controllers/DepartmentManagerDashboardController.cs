using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/department-manager/dashboard")]
[Tags("Department Manager Dashboard")]
[Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager}")]
public class DepartmentManagerDashboardController : ControllerBase
{
    private readonly IDepartmentManagerDashboardService _dashboardService;

    public DepartmentManagerDashboardController(IDepartmentManagerDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private int GetDepartmentId()
    {
        var deptClaim = User.FindFirst("department_id")?.Value;
        return int.TryParse(deptClaim, out var deptId) ? deptId : 0;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentManagerDashboardStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DepartmentManagerDashboardStatsResponse>>> GetStats(CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<DepartmentManagerDashboardStatsResponse> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _dashboardService.GetStatsAsync(deptId, cancellationToken);
        return Ok(new ApiResponse<DepartmentManagerDashboardStatsResponse> { Success = true, Data = result });
    }

    [HttpGet("team-workload")]
    [ProducesResponseType(typeof(ApiResponse<TeamWorkloadResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeamWorkloadResponse>>> GetTeamWorkload(CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<TeamWorkloadResponse> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _dashboardService.GetTeamWorkloadAsync(deptId, cancellationToken);
        return Ok(new ApiResponse<TeamWorkloadResponse> { Success = true, Data = result });
    }

    [HttpGet("unassigned-issues")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentManagerIssueSummary>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentManagerIssueSummary>>>> GetUnassignedIssues(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<IReadOnlyList<DepartmentManagerIssueSummary>> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _dashboardService.GetUnassignedIssuesAsync(deptId, pageNumber, pageSize, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<DepartmentManagerIssueSummary>> { Success = true, Data = result });
    }
}
