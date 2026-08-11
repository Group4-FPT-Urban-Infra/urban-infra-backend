using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.API.Contracts.Issues;
using UrbanInfraSystem.Application.DTOs.Issues;
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
    private readonly IIssueUpvoteService _upvoteService;
    private readonly ICurrentUserService _currentUser;

    public IssuesController(
        IIssueUpvoteService upvoteService,
        ICurrentUserService currentUser)
    {
        _upvoteService = upvoteService;
        _currentUser = currentUser;
    }

    /// <summary>Tạo báo cáo sự cố mới kèm hình ảnh.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Citizen)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public ActionResult<ApiResponse<IssueDetailResponse>> Create([FromForm] CreateIssueFormRequest request)
        => NotImplemented("Tạo báo cáo sự cố");

    /// <summary>Tra cứu các sự cố được phép hiển thị công khai.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>> Search(
        [FromQuery] SearchIssuesRequest request)
        => NotImplemented("Tra cứu sự cố");

    /// <summary>Tìm báo cáo cùng loại gần vị trí để gợi ý tránh tạo trùng.</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NearbyIssueResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearby(
        [FromQuery] FindNearbyIssuesRequest request)
        => NotImplemented("Tìm sự cố gần đây");

    /// <summary>Lấy danh sách báo cáo của công dân đang đăng nhập.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMine(
        [FromQuery] GetMyIssuesRequest request)
        => NotImplemented("Lấy sự cố của tôi");

    /// <summary>Lấy chi tiết một báo cáo sự cố.</summary>
    [HttpGet("{issueId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<IssueDetailResponse>> GetById([FromRoute] long issueId)
        => NotImplemented("Lấy chi tiết sự cố");

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetTimeline(
        [FromRoute] long issueId)
        => NotImplemented("Lấy timeline sự cố");

    private ObjectResult NotImplemented(string feature)
        => Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Chức năng chưa được triển khai",
            detail: $"{feature} hiện chưa được triển khai.");
}
