using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Issues;
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
    private readonly IIssueUpvoteService _upvotes;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(
        IReportService reports,
        IIssueUpvoteService upvotes,
        ICurrentUserService currentUser)
    {
        _reports = reports;
        _upvotes = upvotes;
        _currentUser = currentUser;
    }

    /// <summary>Tạo một báo cáo công dân kèm ảnh và Issue xử lý ban đầu.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Citizen)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<IssueDetailResponse>>> Create(
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
    public async Task<ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>>> Mine(
        [FromQuery] GetMyIssuesRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId)) return Unauthorized();
        return Ok(await _reports.GetMineAsync(request, _currentUser.UserId, cancellationToken));
    }

    /// <summary>Lấy chi tiết Report theo ID; khách chỉ xem được dữ liệu công khai.</summary>
    [HttpGet("{reportId:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IssueDetailResponse>>> GetById(
        long reportId,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GetByIdAsync(reportId, _currentUser.UserId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
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

    /// <summary>Lấy số lượt ủng hộ và trạng thái ủng hộ của người dùng hiện tại.</summary>
    [HttpGet("{reportId:long}/upvote")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> UpvoteStatus(long reportId, CancellationToken cancellationToken)
    {
        var issueId = await _reports.GetPrimaryIssueIdAsync(reportId, cancellationToken);
        if (!issueId.HasValue) return NotFound();
        return Ok(await _upvotes.GetUpvoteStatusAsync(issueId.Value, _currentUser.UserId));
    }

    /// <summary>Ủng hộ Report; một công dân chỉ có một lượt.</summary>
    [HttpPost("{reportId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> Upvote(long reportId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId)) return Unauthorized();
        var issueId = await _reports.GetPrimaryIssueIdAsync(reportId, cancellationToken);
        if (!issueId.HasValue) return NotFound();
        return Ok(await _upvotes.UpvoteAsync(issueId.Value, _currentUser.UserId));
    }

    /// <summary>Bỏ lượt ủng hộ Report.</summary>
    [HttpDelete("{reportId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> RemoveUpvote(long reportId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId)) return Unauthorized();
        var issueId = await _reports.GetPrimaryIssueIdAsync(reportId, cancellationToken);
        if (!issueId.HasValue) return NotFound();
        return Ok(await _upvotes.RemoveUpvoteAsync(issueId.Value, _currentUser.UserId));
    }
}
