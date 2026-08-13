using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.IssueAssignments;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// API cho quản lý báo cáo sự cố hạ tầng đô thị.
/// </summary>
[ApiController]
[Route("api/issues")]
[Tags("Issues")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;
    private readonly IIssueUpvoteService _upvoteService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIssueAssignmentService _assignmentService;

    public IssuesController(
        IIssueService issueService,
        IIssueUpvoteService upvoteService,
        ICurrentUserService currentUser,
        IIssueAssignmentService assignmentService)
    {
        _issueService = issueService;
        _upvoteService = upvoteService;
        _currentUser = currentUser;
        _assignmentService = assignmentService;
    }

    /// <summary>Tạo báo cáo sự cố mới kèm hình ảnh.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Citizen)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IssueDetailResponse>>> Create(
        [FromForm] CreateIssueFormRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Vui lòng đăng nhập với tài khoản công dân để tạo báo cáo sự cố."
            });
        }

        var result = await _issueService.CreateIssueAsync(request, userId, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { issueId = result.Data!.Id }, result);
    }

    /// <summary>Tra cứu các sự cố được phép hiển thị công khai.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>>> Search(
        [FromQuery] SearchIssuesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var result = await _issueService.SearchIssuesAsync(request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Tìm báo cáo cùng loại gần vị trí để gợi ý tránh tạo trùng.</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NearbyIssueResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NearbyIssueResponse>>>> FindNearby(
        [FromQuery] FindNearbyIssuesRequest request,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var result = await _issueService.FindNearbyIssuesAsync(request, userId, ct);
        return Ok(result);
    }

    /// <summary>Lấy danh sách báo cáo của công dân đang đăng nhập.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>>> GetMine(
        [FromQuery] GetMyIssuesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApiResponse<PagedResponse<IssueSummaryResponse>>
            {
                Success = false,
                Message = "Vui lòng đăng nhập để thực hiện thao tác này."
            });
        }

        var result = await _issueService.GetMyIssuesAsync(request, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Lấy chi tiết một báo cáo sự cố.</summary>
    [HttpGet("{issueId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IssueDetailResponse>>> GetById(
        [FromRoute] long issueId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var result = await _issueService.GetIssueByIdAsync(issueId, userId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>Upvote báo cáo; gọi lặp lại không tạo upvote thứ hai.</summary>
    [HttpPost("{issueId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> Upvote([FromRoute] long issueId)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = "Vui lòng đăng nhập để thực hiện thao tác này."
            });
        }

        var result = await _upvoteService.UpvoteAsync(issueId, userId);

        if (!result.Success)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy sự cố",
                Detail = result.Message
            });
        }

        return Ok(result);
    }

    /// <summary>Lấy trạng thái upvote của một báo cáo (số upvote và user đã upvote chưa).</summary>
    [HttpGet("{issueId:long}/upvote")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> GetUpvoteStatus([FromRoute] long issueId)
    {
        var userId = _currentUser.UserId;
        var result = await _upvoteService.GetUpvoteStatusAsync(issueId, userId);

        if (!result.Success)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy sự cố",
                Detail = result.Message
            });
        }

        return Ok(result);
    }

    /// <summary>Bỏ upvote báo cáo; gọi lặp lại vẫn trả trạng thái hiện tại.</summary>
    [HttpDelete("{issueId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> RemoveUpvote([FromRoute] long issueId)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = "Vui lòng đăng nhập để thực hiện thao tác này."
            });
        }

        var result = await _upvoteService.RemoveUpvoteAsync(issueId, userId);

        if (!result.Success)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy sự cố",
                Detail = result.Message
            });
        }

        return Ok(result);
    }

    /// <summary>Lấy lịch sử xử lý công khai của báo cáo.</summary>
    [HttpGet("{issueId:long}/timeline")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>>> GetTimeline(
        [FromRoute] long issueId,
        CancellationToken cancellationToken)
    {
        var result = await _issueService.GetIssueTimelineAsync(issueId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>Lấy lịch sử phân công đơn vị xử lý của sự cố.</summary>
    [HttpGet("{issueId:long}/assignments")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    public async Task<ActionResult<IReadOnlyList<IssueAssignmentResponse>>> GetAssignments(
        [FromRoute] long issueId,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetHistoryAsync(issueId, cancellationToken);
        return result is null
            ? NotFound(new { message = $"Không tìm thấy sự cố có ID = {issueId}." })
            : Ok(result);
    }

    /// <summary>Admin hoặc quản lý đơn vị đang phụ trách chuyển sự cố sang đơn vị khác.</summary>
    [HttpPut("{issueId:long}/reassign")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    public async Task<ActionResult<IssueAssignmentResponse>> Reassign(
        [FromRoute] long issueId,
        [FromBody] ReassignIssueRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        try
        {
            return Ok(await _assignmentService.ReassignAsync(
                issueId, request, userId, _currentUser.IsInRole(Roles.Admin), cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }
}
