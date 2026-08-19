using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.ReRouteRequests;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/re-route-requests")]
[Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager}")]
public class ReRouteRequestsController : ControllerBase
{
    private readonly IReRouteRequestService _reRouteRequestService;

    public ReRouteRequestsController(IReRouteRequestService reRouteRequestService)
    {
        _reRouteRequestService = reRouteRequestService;
    }

    /// <summary>
    /// Lấy danh sách các yêu cầu chuyển tiếp đang chờ duyệt của đơn vị hiện tại.
    /// </summary>
    [HttpGet("incoming")]
    public async Task<ActionResult<IReadOnlyList<ReRouteRequestDto>>> GetIncomingRequests(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Ok(new List<ReRouteRequestDto>());

        // Admin might not have a department_id, but managers do.
        var departmentIdClaim = User.FindFirstValue("department_id");
        if (string.IsNullOrWhiteSpace(departmentIdClaim) || !int.TryParse(departmentIdClaim, out var targetDepartmentId) || targetDepartmentId <= 0)
        {
            // Instead of returning Unauthorized, just return an empty list
            return Ok(new List<ReRouteRequestDto>());
        }

        var requests = await _reRouteRequestService.GetIncomingRequestsAsync(targetDepartmentId, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Chấp nhận yêu cầu chuyển tiếp.
    /// </summary>
    [HttpPost("{id:long}/accept")]
    public async Task<ActionResult> AcceptRequest(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest(new { message = "Vui lòng đăng nhập." });

        try
        {
            await _reRouteRequestService.AcceptRequestAsync(id, userId, cancellationToken);
            return Ok(new { message = "Đã chấp nhận yêu cầu chuyển tiếp thành công." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    /// <summary>
    /// Từ chối yêu cầu chuyển tiếp.
    /// </summary>
    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult> RejectRequest(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest(new { message = "Vui lòng đăng nhập." });

        try
        {
            await _reRouteRequestService.RejectRequestAsync(id, userId, cancellationToken);
            return Ok(new { message = "Đã từ chối yêu cầu chuyển tiếp." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
}
