using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.API.Contracts.Issues;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>Contract API cho báo cáo và tra cứu sự cố hạ tầng đô thị.</summary>
/// <remarks>
/// Các action hiện khai báo contract để Swagger và Frontend thống nhất trước khi
/// entity/service/database của module Issue được triển khai.
/// </remarks>
[ApiController]
[Route("api/issues")]
[Tags("Issues")]
public class IssuesController : ControllerBase
{
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<IssueDetailResponse>> Create([FromForm] CreateIssueFormRequest request)
        => ContractOnly();

    /// <summary>Tra cứu các sự cố được phép hiển thị công khai.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>> Search(
        [FromQuery] SearchIssuesRequest request)
        => ContractOnly();

    /// <summary>Tìm báo cáo cùng loại gần vị trí để gợi ý tránh tạo trùng.</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NearbyIssueResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearby(
        [FromQuery] FindNearbyIssuesRequest request)
        => ContractOnly();

    /// <summary>Lấy danh sách báo cáo của công dân đang đăng nhập.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IssueSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMine(
        [FromQuery] GetMyIssuesRequest request)
        => ContractOnly();

    /// <summary>Lấy chi tiết một báo cáo sự cố.</summary>
    [HttpGet("{issueId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IssueDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<IssueDetailResponse>> GetById([FromRoute] long issueId)
        => ContractOnly();

    /// <summary>Upvote báo cáo; gọi lặp lại không tạo upvote thứ hai.</summary>
    [HttpPost("{issueId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<UpvoteResponse>> Upvote([FromRoute] long issueId)
        => ContractOnly();

    /// <summary>Bỏ upvote báo cáo; gọi lặp lại vẫn trả trạng thái hiện tại.</summary>
    [HttpDelete("{issueId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<UpvoteResponse>> RemoveUpvote([FromRoute] long issueId)
        => ContractOnly();

    /// <summary>Lấy lịch sử xử lý công khai của báo cáo.</summary>
    [HttpGet("{issueId:long}/timeline")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetTimeline(
        [FromRoute] long issueId)
        => ContractOnly();

    private ObjectResult ContractOnly() => Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Issue API chưa được triển khai",
        detail: "Endpoint hiện mới được khai báo để thống nhất API contract trên Swagger.");
}
