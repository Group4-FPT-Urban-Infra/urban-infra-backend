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
[Route("api/department-manager/issues")]
[Tags("Department Manager Issues")]
[Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager}")]
public class DepartmentManagerIssuesController : ControllerBase
{
    private readonly IDepartmentManagerIssueService _issueService;
    private readonly ICurrentUserService _currentUser;

    public DepartmentManagerIssuesController(
        IDepartmentManagerIssueService issueService,
        ICurrentUserService currentUser)
    {
        _issueService = issueService;
        _currentUser = currentUser;
    }

    private int GetDepartmentId()
    {
        var deptClaim = User.FindFirst("department_id")?.Value;
        return int.TryParse(deptClaim, out var deptId) ? deptId : 0;
    }

    [HttpPost("filter")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<DepartmentManagerIssueSummary>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DepartmentManagerIssueSummary>>>> GetIssues(
        [FromBody] DepartmentManagerIssueListRequest request,
        CancellationToken cancellationToken)
    {
        var deptId = GetDepartmentId();
        if (deptId == 0) return Ok(new ApiResponse<PaginatedResponse<DepartmentManagerIssueSummary>> { Success = false, Message = "Khong co thong tin don vi." });

        var result = await _issueService.GetIssuesAsync(deptId, request, cancellationToken);
        return Ok(new ApiResponse<PaginatedResponse<DepartmentManagerIssueSummary>> { Success = true, Data = result });
    }

    [HttpGet("{issueId:long}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentManagerIssueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DepartmentManagerIssueDetailResponse>>> GetIssueDetail(
        [FromRoute] long issueId,
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetIssueDetailAsync(issueId, cancellationToken);
        if (result is null) return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = $"Khong tim thay su co ID = {issueId}." });

        return Ok(new ApiResponse<DepartmentManagerIssueDetailResponse> { Success = true, Data = result });
    }

    [HttpPost("{issueId:long}/assign")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentManagerIssueDetailResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DepartmentManagerIssueDetailResponse>>> AssignIssue(
        [FromRoute] long issueId,
        [FromBody] AssignIssueRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId)) return Ok(new ApiResponse<DepartmentManagerIssueDetailResponse> { Success = false, Message = "Vui long dang nhap." });

        try
        {
            var result = await _issueService.AssignIssueAsync(issueId, request, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DepartmentManagerIssueDetailResponse> { Success = true, Data = result, Message = "Gan nhan vien thanh cong." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = ex.Message });
        }
    }

    [HttpPut("{issueId:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentManagerIssueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DepartmentManagerIssueDetailResponse>>> UpdateStatus(
        [FromRoute] long issueId,
        [FromBody] DepartmentManagerUpdateIssueStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId)) return Ok(new ApiResponse<DepartmentManagerIssueDetailResponse> { Success = false, Message = "Vui long dang nhap." });

        try
        {
            var result = await _issueService.UpdateIssueStatusAsync(issueId, request, userId, cancellationToken);
            return Ok(new ApiResponse<DepartmentManagerIssueDetailResponse> { Success = true, Data = result, Message = "Cap nhat trang thai thanh cong." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = ex.Message });
        }
    }
}
