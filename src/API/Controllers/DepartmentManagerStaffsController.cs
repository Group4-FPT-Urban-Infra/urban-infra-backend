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
[Route("api/department-manager/staffs")]
[Tags("Department Manager Staffs")]
[Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager}")]
public class DepartmentManagerStaffsController : ControllerBase
{
    private readonly IDepartmentManagerStaffService _staffService;

    public DepartmentManagerStaffsController(IDepartmentManagerStaffService staffService) => _staffService = staffService;

    private int GetDepartmentId()
    {
        var deptClaim = User.FindFirst("department_id")?.Value;
        return int.TryParse(deptClaim, out var deptId) ? deptId : 0;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StaffMemberResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StaffMemberResponse>>>> GetStaffs(CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Unauthorized(new ApiResponse<IReadOnlyList<StaffMemberResponse>> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _staffService.GetStaffsAsync(deptId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<StaffMemberResponse>> { Success = true, Data = result });
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<StaffDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StaffDetailResponse>>> GetStaffDetail(
        [FromRoute] string userId,
        CancellationToken cancellationToken)
    {
        var result = await _staffService.GetStaffDetailAsync(userId, cancellationToken);
        if (result is null) return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = $"Khong tim thay nhan vien ID = {userId}." });

        return Ok(new ApiResponse<StaffDetailResponse> { Success = true, Data = result });
    }
}
