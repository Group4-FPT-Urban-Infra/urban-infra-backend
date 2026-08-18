using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.IssueAssignmentMembers;
using UrbanInfraSystem.Application.DTOs.IssueAssignments;
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
    private readonly IIssueService _issueService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIssueAssignmentService _assignmentService;
    private readonly IIssueAssignmentMemberService _memberService;
    private readonly IIssueUpvoteService _upvoteService;

    public IssuesController(
        IIssueService issueService,
        ICurrentUserService currentUser,
        IIssueAssignmentService assignmentService,
        IIssueAssignmentMemberService memberService,
        IIssueUpvoteService upvoteService)
    {
        _issueService = issueService;
        _currentUser = currentUser;
        _assignmentService = assignmentService;
        _memberService = memberService;
        _upvoteService = upvoteService;
    }

    /// <summary>Toggle upvote cho một báo cáo sự cố (thêm nếu chưa upvote, bỏ nếu đã upvote).</summary>
    [HttpPost("{issueId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> ToggleUpvote(
        [FromRoute] long issueId,
        CancellationToken cancellationToken)
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

        // Verify issue exists
        var issue = await _issueService.GetIssueByIdAsync(issueId, userId, cancellationToken);
        if (!issue.Success)
        {
            return NotFound(new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = $"Không tìm thấy sự cố có ID = {issueId}."
            });
        }

        var hasUpvoted = await _upvoteService.ToggleAsync(issueId, userId, cancellationToken);

        return Ok(new ApiResponse<UpvoteResponse>
        {
            Success = true,
            Data = new UpvoteResponse
            {
                IssueId = issueId,
                HasUpvoted = hasUpvoted,
                UpvoteCount = issue.Data!.UpvoteCount + (hasUpvoted ? 1 : 0)
            }
        });
    }

    /// <summary>Xóa upvote khỏi một báo cáo sự cố.</summary>
    [HttpDelete("{issueId:long}/upvote")]
    [Authorize(Roles = Roles.Citizen)]
    [ProducesResponseType(typeof(ApiResponse<UpvoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UpvoteResponse>>> RemoveUpvote(
        [FromRoute] long issueId,
        CancellationToken cancellationToken)
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

        // Verify issue exists
        var issue = await _issueService.GetIssueByIdAsync(issueId, userId, cancellationToken);
        if (!issue.Success)
        {
            return NotFound(new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = $"Không tìm thấy sự cố có ID = {issueId}."
            });
        }

        await _upvoteService.RemoveAsync(issueId, userId, cancellationToken);

        return Ok(new ApiResponse<UpvoteResponse>
        {
            Success = true,
            Data = new UpvoteResponse
            {
                IssueId = issueId,
                HasUpvoted = false,
                UpvoteCount = Math.Max(0, issue.Data!.UpvoteCount - 1)
            }
        });
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
    [HttpPost("{issueId:long}/re-route")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager}")]
    public async Task<ActionResult<IssueAssignmentResponse>> ReRoute(
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

    /// <summary>Lấy danh sách nhân viên được gán vào một phân công cụ thể.</summary>
    [HttpGet("{issueId:long}/assignments/{assignmentId:long}/members")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    [ProducesResponseType(typeof(IReadOnlyList<IssueAssignmentMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<IssueAssignmentMemberResponse>>> GetAssignmentMembers(
        [FromRoute] long issueId,
        [FromRoute] long assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await _memberService.GetByAssignmentAsync(assignmentId, cancellationToken);
        return result is null
            ? NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Không tìm thấy phân công." })
            : Ok(result);
    }

    /// <summary>Gán một nhân viên vào phân công (chỉ Manager hoặc Admin).</summary>
    [HttpPost("{issueId:long}/assignments/{assignmentId:long}/members")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    [ProducesResponseType(typeof(IssueAssignmentMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IssueAssignmentMemberResponse>> AssignMember(
        [FromRoute] long issueId,
        [FromRoute] long assignmentId,
        [FromBody] AssignMemberRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        try
        {
            var result = await _memberService.AssignMemberAsync(assignmentId, request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetAssignmentMembers), new { issueId, assignmentId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = ex.Message });
        }
    }

    /// <summary>Cập nhật trạng thái của nhân viên trong phân công (chấp nhận, từ chối, hoàn thành).</summary>
    [HttpPut("{issueId:long}/assignments/{assignmentId:long}/members/{memberId:long}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    [ProducesResponseType(typeof(IssueAssignmentMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IssueAssignmentMemberResponse>> UpdateMemberStatus(
        [FromRoute] long issueId,
        [FromRoute] long assignmentId,
        [FromRoute] long memberId,
        [FromBody] UpdateMemberStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        try
        {
            var result = await _memberService.UpdateMemberStatusAsync(memberId, request, userId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = ex.Message });
        }
    }

    /// <summary>Xóa nhân viên khỏi phân công.</summary>
    [HttpDelete("{issueId:long}/assignments/{assignmentId:long}/members/{memberId:long}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] long issueId,
        [FromRoute] long assignmentId,
        [FromRoute] long memberId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        try
        {
            await _memberService.RemoveMemberAsync(memberId, userId, cancellationToken);
            return NoContent();
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
}
