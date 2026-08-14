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
[Route("api/department-manager/sla")]
[Tags("Department Manager SLA")]
[Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager}")]
public class DepartmentManagerSlaController : ControllerBase
{
    private readonly IDepartmentManagerSlaService _slaService;

    public DepartmentManagerSlaController(IDepartmentManagerSlaService slaService) => _slaService = slaService;

    private int GetDepartmentId()
    {
        var deptClaim = User.FindFirst("department_id")?.Value;
        return int.TryParse(deptClaim, out var deptId) ? deptId : 0;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<SlaOverviewResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SlaOverviewResponse>>> GetOverview(CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<SlaOverviewResponse> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _slaService.GetOverviewAsync(deptId, cancellationToken);
        return Ok(new ApiResponse<SlaOverviewResponse> { Success = true, Data = result });
    }

    [HttpGet("near-deadline")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SlaNearDeadlineItem>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SlaNearDeadlineItem>>>> GetNearDeadline(CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<IReadOnlyList<SlaNearDeadlineItem>> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _slaService.GetNearDeadlineAsync(deptId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<SlaNearDeadlineItem>> { Success = true, Data = result });
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<SlaHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SlaHistoryResponse>>> GetHistory(
        [FromQuery] int months,
        CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<SlaHistoryResponse> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _slaService.GetHistoryAsync(deptId, months, cancellationToken);
        return Ok(new ApiResponse<SlaHistoryResponse> { Success = true, Data = result });
    }
}
