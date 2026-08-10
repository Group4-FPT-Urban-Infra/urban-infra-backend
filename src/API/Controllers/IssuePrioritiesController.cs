using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.IssuePriorities;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// CRUD API quản lý mức độ ưu tiên sự cố (Issue_Priorities).
/// - GET (danh sách / chi tiết): Public – không cần đăng nhập.
/// - POST / PATCH / DELETE: Chỉ Admin.
/// </summary>
[ApiController]
[Route("api/issue-priorities")]
[Tags("Issue Priorities")]
public class IssuePrioritiesController : ControllerBase
{
    private readonly IIssuePriorityService _service;

    public IssuePrioritiesController(IIssuePriorityService service)
    {
        _service = service;
    }

    // ─── GET /api/issue-priorities ────────────────────────────────────────────

    /// <summary>Lấy danh sách tất cả mức ưu tiên (kể cả đã vô hiệu hoá).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<IssuePriorityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IssuePriorityResponse>>> GetAll(
        CancellationToken ct,
        [FromQuery] bool activeOnly = false)
    {
        var result = activeOnly
            ? await _service.GetActiveAsync(ct)
            : await _service.GetAllAsync(ct);

        return Ok(result);
    }

    // ─── GET /api/issue-priorities/{id} ──────────────────────────────────────

    /// <summary>Lấy chi tiết một mức ưu tiên theo ID.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IssuePriorityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssuePriorityResponse>> GetById([FromRoute] int id, CancellationToken ct)
    {
        if (id is < 1 or > 255) return NotFound();

        var result = await _service.GetByIdAsync((byte)id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ─── POST /api/issue-priorities ──────────────────────────────────────────

    /// <summary>Tạo mới một mức ưu tiên. Chỉ Admin.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IssuePriorityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IssuePriorityResponse>> Create(
        [FromBody] CreateIssuePriorityRequest request,
        CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.PriorityId }, created);
    }

    // ─── PATCH /api/issue-priorities/{id} ────────────────────────────────────

    /// <summary>Cập nhật một mức ưu tiên (PATCH – chỉ field có giá trị mới được cập nhật). Chỉ Admin.</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IssuePriorityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssuePriorityResponse>> Update(
        [FromRoute] int id,
        [FromBody] UpdateIssuePriorityRequest request,
        CancellationToken ct)
    {
        if (id is < 1 or > 255) return NotFound();

        var updated = await _service.UpdateAsync((byte)id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    // ─── DELETE /api/issue-priorities/{id}/deactivate ────────────────────────

    /// <summary>Vô hiệu hoá (soft-delete) một mức ưu tiên. Chỉ Admin.</summary>
    [HttpDelete("{id:int}/deactivate")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([FromRoute] int id, CancellationToken ct)
    {
        if (id is < 1 or > 255) return NotFound();

        var success = await _service.DeactivateAsync((byte)id, ct);
        return success ? NoContent() : NotFound();
    }

    // ─── DELETE /api/issue-priorities/{id} ───────────────────────────────────

    /// <summary>Xoá vĩnh viễn một mức ưu tiên khỏi database. Chỉ Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        if (id is < 1 or > 255) return NotFound();

        var success = await _service.DeleteAsync((byte)id, ct);
        return success ? NoContent() : NotFound();
    }
}
