using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Staff;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Application.DTOs.Issues;
using System.Collections.Generic;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// API cho các nghiệp vụ của Cán bộ xử lý (Department Staff).
/// </summary>
[ApiController]
[Route("api/staff")]
[Tags("Staff")]
[Authorize(Roles = Roles.DepartmentStaff)]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;
    private readonly ICurrentUserService _currentUser;

    public StaffController(IStaffService staffService, ICurrentUserService currentUser)
    {
        _staffService = staffService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Lấy dữ liệu tổng quan cho dashboard của cán bộ xử lý.
    /// Dữ liệu bao gồm các số liệu về sự cố được phân công cho đơn vị của cán bộ.
    /// </summary>
    [HttpGet("dashboard/summary")]
    [ProducesResponseType(typeof(StaffDashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StaffDashboardSummaryResponse>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var summary = await _staffService.GetDashboardSummaryAsync(userId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Lấy danh sách các công việc (sự cố) đang mở được giao cho đơn vị của cán bộ.
    /// </summary>
    [HttpGet("tasks/my")]
    [ProducesResponseType(typeof(List<StaffTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<StaffTaskResponse>>> GetMyTasks(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            // This check is redundant due to [Authorize] but good for safety.
            return Unauthorized();
        }

        var tasks = await _staffService.GetMyTasksAsync(userId, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>
    /// Lấy các hoạt động gần đây trên các sự cố được giao cho đơn vị của cán bộ.
    /// </summary>
    [HttpGet("activities/recent")]
    [ProducesResponseType(typeof(List<StaffActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<StaffActivityResponse>>> GetMyRecentActivities(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            // This check is redundant due to [Authorize] but good for safety.
            return Unauthorized();
        }

        var activities = await _staffService.GetMyRecentActivitiesAsync(userId, cancellationToken);
        return Ok(activities);
    }

    /// <summary>
    /// Lấy danh sách sự cố có phân trang và bộ lọc, trong phạm vi của cán bộ.
    /// </summary>
    [HttpGet("incidents")]
    [ProducesResponseType(typeof(PagedResponse<StaffIncidentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<StaffIncidentResponse>>> GetStaffIncidents(
        [FromQuery] StaffIncidentFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            // This check is redundant due to [Authorize] but good for safety.
            return Unauthorized();
        }

        var result = await _staffService.GetIncidentsAsync(userId, filters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách sự cố để hiển thị trên bản đồ của cán bộ, có áp dụng bộ lọc.
    /// </summary>
    [HttpGet("issues/map")]
    [ProducesResponseType(typeof(List<StaffMapIssueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<StaffMapIssueResponse>>> GetStaffMapIssues(
        [FromQuery] StaffIncidentFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            // This check is redundant due to [Authorize] but good for safety.
            return Unauthorized();
        }

        var result = await _staffService.GetMapIssuesAsync(userId, filters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cán bộ tự nhận một sự cố chưa được phân công.
    /// </summary>
    [HttpPost("incidents/{issueId:long}/claim")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimIncident(long issueId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _staffService.ClaimIncidentAsync(issueId, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }
}