using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Reports;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// API cấp Report: một nội dung do công dân gửi và có thể sinh một hoặc nhiều Issue nghiệp vụ.
/// </summary>
[ApiController]
[Route("api/reports")]
[Tags("Reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IReportService reports, ICurrentUserService currentUser)
    {
        _reports = reports;
        _currentUser = currentUser;
    }

    /// <summary>Tạo một báo cáo công dân kèm ảnh và Issue xử lý ban đầu.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Citizen)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ReportDetailResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ReportDetailResponse>>> Create(
        [FromForm] CreateIssueFormRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId)) return Unauthorized();

        var result = await _reports.CreateAsync(request, _currentUser.UserId, cancellationToken);
        if (!result.Success) return BadRequest(result);

        var reportId = result.Data!.Id;
        return CreatedAtAction(nameof(GetById), new { reportId }, result);
    }

    /// <summary>Tra cứu các báo cáo được công khai; khách không cần đăng nhập.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>>> Search(
        [FromQuery] SearchIssuesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _reports.SearchAsync(request, _currentUser.UserId, cancellationToken));

    /// <summary>Tìm các báo cáo cùng loại ở gần vị trí; khách không cần đăng nhập.</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NearbyIssueResponse>>>> Nearby(
        [FromQuery] FindNearbyIssuesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _reports.FindNearbyAsync(request, _currentUser.UserId, cancellationToken));

    /// <summary>Lấy các báo cáo của công dân đang đăng nhập.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Citizen)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ReportSummaryResponse>>>> Mine(
        [FromQuery] GetMyIssuesRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId)) return Unauthorized();
        return Ok(await _reports.GetMineAsync(request, _currentUser.UserId, cancellationToken));
    }

    /// <summary>Lấy chi tiết Report theo ID; khách chỉ xem được dữ liệu công khai.</summary>
    [HttpGet("{reportId:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ReportDetailResponse>>> GetById(
        long reportId,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GetByIdAsync(reportId, _currentUser.UserId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Các cập nhật mới nhất trên những Issue thuộc Report của công dân.</summary>
    [HttpGet("mine/updates")]
    [Authorize(Roles = Roles.Citizen)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportUpdateResponse>>>> MyUpdates(
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId)) return Unauthorized();
        return Ok(await _reports.GetMyRecentUpdatesAsync(_currentUser.UserId, limit, cancellationToken));
    }

    /// <summary>Lấy lịch sử trạng thái của tất cả Issue thuộc Report.</summary>
    [HttpGet("{reportId:long}/timeline")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>>> Timeline(
        long reportId,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GetTimelineAsync(reportId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
