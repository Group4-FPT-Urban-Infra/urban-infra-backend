using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/departments")]
[Tags("Departments")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;
    private readonly IDepartmentMemberService _members;

    public DepartmentsController(IDepartmentService departments, IDepartmentMemberService members)
    {
        _departments = departments;
        _members = members;
    }

    /// <summary>Lấy danh sách đơn vị; hỗ trợ lọc theo đơn vị cha, trạng thái và từ khóa.</summary>
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<DepartmentResponse>>> GetDepartments(
        [FromQuery] DepartmentFilterRequest filter, CancellationToken cancellationToken)
        => Ok(await _departments.GetDepartmentsAsync(filter, cancellationToken));

    /// <summary>Lấy chi tiết đơn vị và các đơn vị con trực tiếp.</summary>
    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<ActionResult<DepartmentResponse>> GetDepartment(int id, CancellationToken cancellationToken)
    {
        var result = await _departments.GetDepartmentByIdAsync(id, cancellationToken);
        return result is null ? NotFound(new { message = $"Không tìm thấy đơn vị có ID = {id}." }) : Ok(result);
    }

    /// <summary>Tạo đơn vị; parentDepartmentId dùng để tạo cây đơn vị.</summary>
    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment(
        [FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _departments.CreateDepartmentAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetDepartment), new { id = result.DepartmentId }, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Cập nhật toàn bộ thông tin một đơn vị.</summary>
    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(
        int id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _departments.UpdateDepartmentAsync(id, request, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Xóa đơn vị nếu không còn đơn vị con hoặc cán bộ đang hoạt động.</summary>
    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteDepartment(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _departments.DeleteDepartmentAsync(id, cancellationToken)
                ? NoContent()
                : NotFound(new { message = $"Không tìm thấy đơn vị có ID = {id}." });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Lấy danh sách cán bộ của đơn vị.</summary>
    [HttpGet("{id:int}/members"), Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentStaff}")]
    public async Task<ActionResult<List<DepartmentMemberResponse>>> GetMembers(
        int id, [FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try { return Ok(await _members.GetMembersAsync(id, activeOnly, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Gán người dùng có vai trò DepartmentStaff vào đơn vị.</summary>
    [HttpPost("{id:int}/members"), Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<DepartmentMemberResponse>> AssignMember(
        int id, [FromBody] AssignDepartmentMemberRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _members.AssignMemberAsync(id, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Gỡ cán bộ khỏi đơn vị, đồng thời lưu leftAt để giữ lịch sử.</summary>
    [HttpDelete("{id:int}/members"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RemoveMember(
        int id, [FromQuery] string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _members.RemoveMemberAsync(id, userId, cancellationToken)
                ? NoContent()
                : NotFound(new { message = "Không tìm thấy cán bộ đang hoạt động trong đơn vị này." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Lấy thông tin đơn vị của người dùng hiện tại (trưởng phòng/nhân viên).</summary>
    [HttpGet("me/department"), Authorize(Roles = $"{Roles.Admin},{Roles.DepartmentManager},{Roles.DepartmentStaff}")]
    public async Task<ActionResult<DepartmentMemberResponse>> GetCurrentUserDepartment(CancellationToken cancellationToken)
    {
        var result = await _members.GetCurrentUserDepartmentAsync(cancellationToken);
        return result is null ? NotFound(new { message = "Người dùng hiện tại không thuộc đơn vị nào." }) : Ok(result);
    }
}
