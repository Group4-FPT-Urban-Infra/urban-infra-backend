using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Staff;
using UrbanInfraSystem.Application.Interfaces;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "DEPARTMENT_STAFF,Staff,ADMIN")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;
    private readonly ICurrentUserService _currentUserService;

    public StaffController(IStaffService staffService, ICurrentUserService currentUserService)
    {
        _staffService = staffService;
        _currentUserService = currentUserService;
    }

    private string GetCurrentStaffId()
    {
        return _currentUserService.UserId 
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
               ?? User.FindFirstValue("sub") 
               ?? string.Empty;
    }

    /// <summary>
    /// Lấy các số liệu tổng quan cho dashboard của nhân viên.
    /// </summary>
    [HttpGet("dashboard/summary")]
    [ProducesResponseType(typeof(StaffDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetDashboardSummaryAsync(staffId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy thông tin phòng ban cho nhân viên." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các công việc đã chấp nhận của nhân viên hiện tại có phân trang.
    /// </summary>
    [HttpGet("tasks/my")]
    [ProducesResponseType(typeof(PagedResponse<StaffTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyTasks([FromQuery] GetMyTasksRequest request, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetMyTasksAsync(request, staffId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy các hoạt động gần đây trong phòng ban của nhân viên.
    /// </summary>
    [HttpGet("activities/recent")]
    [ProducesResponseType(typeof(PagedResponse<StaffActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyRecentActivities([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetMyRecentActivitiesAsync(request, staffId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy thông tin phòng ban cho nhân viên." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các phân công đang chờ nhân viên hiện tại phản hồi.
    /// </summary>
    [HttpGet("assignments/pending")]
    [ProducesResponseType(typeof(PagedResponse<StaffAssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyPendingAssignments([FromQuery] GetMyAssignmentsRequest request, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetMyPendingAssignmentsAsync(request, staffId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Nhân viên chấp nhận hoặc từ chối một phân công.
    /// </summary>
    [HttpPost("assignments/{assignmentId:long}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RespondToAssignment(
        long assignmentId, 
        [FromBody] RespondToAssignmentRequest request, 
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        try
        {
            await _staffService.RespondToAssignmentAsync(assignmentId, request, staffId, cancellationToken);
            return Ok(new { success = true, data = true, message = "Phản hồi phân công thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách các sự cố trong một khung nhìn bản đồ cho nhân viên.
    /// </summary>
    [HttpGet("issues/map")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffMapIssueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMapIssues([FromQuery] GetMapIssuesRequest request, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetMapIssuesAsync(request, staffId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy thông tin phòng ban cho nhân viên." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách các sự cố thuộc phòng ban của nhân viên hiện tại.
    /// </summary>
    [HttpGet("incidents")]
    [ProducesResponseType(typeof(PagedResponse<StaffIncidentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncidents([FromQuery] SearchStaffIncidentsRequest request, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetIncidentsAsync(request, staffId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy thông tin phòng ban cho nhân viên." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một sự cố thuộc phòng ban của nhân viên hiện tại.
    /// </summary>
    [HttpGet("incidents/{issueId:long}")]
    [ProducesResponseType(typeof(StaffIncidentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncidentDetail(long issueId, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        var result = await _staffService.GetIncidentDetailAsync(issueId, staffId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy sự cố ID {issueId} hoặc bạn không có quyền xem." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Nhân viên tự nhận (claim) một sự cố chưa được gán.
    /// </summary>
    [HttpPost("incidents/{issueId:long}/claim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimIncident(long issueId, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentStaffId();
        try
        {
            await _staffService.ClaimIncidentAsync(issueId, staffId, cancellationToken);
            return Ok(new { success = true, message = "Nhận xử lý sự cố thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }
}