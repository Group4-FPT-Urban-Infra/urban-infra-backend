using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.IssueStatuses;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// CRUD API quản lý trạng thái sự cố (Issue_Statuses).
/// - GET (danh sách / chi tiết): Public – không cần đăng nhập.
/// - POST / PATCH / DELETE: Chỉ Admin.
/// </summary>
[ApiController]
[Route("api/issue-statuses")]
[Tags("Issue Statuses")]
public class IssueStatusesController : ControllerBase
{
    private readonly IIssueStatusService _service;

    public IssueStatusesController(IIssueStatusService service)
    {
        _service = service;
    }

    // ─── GET /api/issue-statuses ──────────────────────────────────────────────

    /// <summary>Lấy danh sách tất cả trạng thái sự cố (kể cả đã vô hiệu hoá).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<IssueStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IssueStatusResponse>>> GetAll(
        CancellationToken ct,
        [FromQuery] bool activeOnly = false)
    {
        var result = activeOnly
            ? await _service.GetActiveAsync(ct)
            : await _service.GetAllAsync(ct);

        return Ok(result);
    }

    // ─── GET /api/issue-statuses/{id} ────────────────────────────────────────

    /// <summary>Lấy chi tiết một trạng thái sự cố theo ID.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IssueStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueStatusResponse>> GetById([FromRoute] int id, CancellationToken ct)
    {
        if (id < 1) return NotFound();

        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ─── POST /api/issue-statuses ─────────────────────────────────────────────

    /// <summary>Tạo mới một trạng thái sự cố. Chỉ Admin.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IssueStatusResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IssueStatusResponse>> Create(
        [FromBody] CreateIssueStatusRequest request,
        CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.StatusId }, created);
    }

    // ─── PATCH /api/issue-statuses/{id} ──────────────────────────────────────

    /// <summary>Cập nhật một trạng thái sự cố (PATCH – chỉ field có giá trị mới được cập nhật). Chỉ Admin.</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IssueStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IssueStatusResponse>> Update(
        [FromRoute] int id,
        [FromBody] UpdateIssueStatusRequest request,
        CancellationToken ct)
    {
        if (id < 1) return NotFound();

        var updated = await _service.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    // ─── DELETE /api/issue-statuses/{id}/deactivate ───────────────────────────

    /// <summary>Vô hiệu hoá (soft-delete) một trạng thái sự cố. Chỉ Admin.</summary>
    [HttpDelete("{id:int}/deactivate")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([FromRoute] int id, CancellationToken ct)
    {
        if (id < 1) return NotFound();

        var success = await _service.DeactivateAsync(id, ct);
        return success ? NoContent() : NotFound();
    }

    // ─── DELETE /api/issue-statuses/{id} ─────────────────────────────────────

    /// <summary>Xoá vĩnh viễn một trạng thái sự cố khỏi database. Chỉ Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        if (id < 1) return NotFound();

        var success = await _service.DeleteAsync(id, ct);
        return success ? NoContent() : NotFound();
    }
}
