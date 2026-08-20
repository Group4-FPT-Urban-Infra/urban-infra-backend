using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Staff;
using UrbanInfraSystem.Application.DTOs.IssueAssignmentMembers;
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
    private readonly IIssueAssignmentMemberService _memberService;
    private readonly ICurrentUserService _currentUser;

    public StaffController(
        IStaffService staffService,
        IIssueAssignmentMemberService memberService,
        ICurrentUserService currentUser)
    {
        _staffService = staffService;
        _memberService = memberService;
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
            return Unauthorized();
        }

        var activities = await _staffService.GetMyRecentActivitiesAsync(userId, cancellationToken);
        return Ok(activities);
    }

    /// <summary>
    /// Lấy danh sách các sự cố có phân trang và bộ lọc, trong phạm vi của cán bộ.
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
        [FromQuery] StaffMapFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
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

    /// <summary>
    /// Lấy chi tiết một sự cố để hiển thị trên trang detail của cán bộ.
    /// </summary>
    [HttpGet("incidents/{issueId:long}")]
    [ProducesResponseType(typeof(StaffIncidentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffIncidentDetailResponse>> GetIncidentDetail(
        long issueId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Sự cố không tìm thấy hoặc bạn không có quyền truy cập." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Cán bộ cập nhật trạng thái sự cố và/hoặc tải lên bằng chứng xử lý.
    /// </summary>
    [HttpPost("incidents/{issueId:long}/evidence")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(StaffIncidentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffIncidentDetailResponse>> UploadEvidence(
        long issueId,
        [FromForm] StaffUpdateIssueRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _staffService.UpdateIssueAsync(issueId, request, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cán bộ chấp nhận phân công xử lý sự cố.
    /// </summary>
    [HttpPost("incidents/{issueId:long}/accept")]
    [ProducesResponseType(typeof(StaffIncidentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffIncidentDetailResponse>> AcceptAssignment(
        long issueId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var incident = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
        if (incident == null) return NotFound(new { message = "Sự cố không tìm thấy hoặc bạn không có quyền." });
        if (incident.CurrentMember == null) return NotFound(new { message = "Bạn chưa được gán vào sự cố này." });

        try
        {
            await _memberService.UpdateMemberStatusAsync(
                incident.CurrentMember.MemberId,
                new UpdateMemberStatusRequest { Status = "ACCEPTED" },
                userId,
                cancellationToken);

            var updated = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>
    /// Cán bộ từ chối phân công xử lý sự cố.
    /// </summary>
    [HttpPost("incidents/{issueId:long}/reject")]
    [ProducesResponseType(typeof(StaffIncidentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffIncidentDetailResponse>> RejectAssignment(
        long issueId,
        [FromBody] RejectAssignmentRequest? body,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var incident = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
        if (incident == null) return NotFound(new { message = "Sự cố không tìm thấy hoặc bạn không có quyền." });
        if (incident.CurrentMember == null) return NotFound(new { message = "Bạn chưa được gán vào sự cố này." });

        try
        {
            await _memberService.UpdateMemberStatusAsync(
                incident.CurrentMember.MemberId,
                new UpdateMemberStatusRequest { Status = "REJECTED", Note = body?.Note },
                userId,
                cancellationToken);

            var updated = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>
    /// Cán bộ đánh dấu đã hoàn thành công việc xử lý sự cố.
    /// </summary>
    [HttpPost("incidents/{issueId:long}/complete")]
    [ProducesResponseType(typeof(StaffIncidentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffIncidentDetailResponse>> CompleteAssignment(
        long issueId,
        [FromBody] CompleteAssignmentRequest? body,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var incident = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
        if (incident == null) return NotFound(new { message = "Sự cố không tìm thấy hoặc bạn không có quyền." });
        if (incident.CurrentMember == null) return NotFound(new { message = "Bạn chưa được gán vào sự cố này." });

        try
        {
            await _memberService.MarkCompleteAsync(
                incident.CurrentMember.MemberId,
                userId,
                body?.Note,
                cancellationToken);

            var updated = await _staffService.GetIncidentDetailAsync(issueId, userId, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
