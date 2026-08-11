using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.UserManagement;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// Quản lý User dành cho Quản trị viên (Admin).
/// Tất cả endpoints đều yêu cầu JWT hợp lệ với role Admin.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Tags("Admin – User Management")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    // ------------------------------------------------------------------ //
    //  GET LIST
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Lấy danh sách user có phân trang.
    /// Hỗ trợ lọc theo role, trạng thái hoạt động, phòng ban và từ khóa tìm kiếm.
    /// </summary>
    /// <param name="filter">Bộ lọc và tham số phân trang.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Kết quả phân trang gồm danh sách user và metadata.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserResponse>>> GetUsers(
        [FromQuery] AdminUserListRequest filter, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.GetUsersAsync(filter, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ------------------------------------------------------------------ //
    //  GET BY ID
    // ------------------------------------------------------------------ //

    /// <summary>Lấy thông tin chi tiết của một user theo ID.</summary>
    /// <param name="id">Identity user ID (GUID dạng string).</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserResponse>> GetUser(
        string id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(new { message = $"Không tìm thấy user có ID = {id}." })
            : Ok(result);
    }

    // ------------------------------------------------------------------ //
    //  CREATE
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Tạo tài khoản mới với role chỉ định.
    /// Admin có thể tạo user với bất kỳ role nào (Admin / DepartmentStaff / Citizen).
    /// </summary>
    /// <param name="request">Thông tin tài khoản mới.</param>
    [HttpPost]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        [FromBody] CreateUserByAdminRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ------------------------------------------------------------------ //
    //  UPDATE
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Cập nhật thông tin user: họ tên, số điện thoại, role và phòng ban.
    /// Role cũ sẽ bị thay thế hoàn toàn bằng role mới.
    /// </summary>
    /// <param name="id">Identity user ID.</param>
    /// <param name="request">Thông tin cập nhật.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        string id, [FromBody] UpdateUserByAdminRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ------------------------------------------------------------------ //
    //  LOCK / UNLOCK
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Khoá hoặc mở khoá tài khoản user.
    /// User bị khoá (IsActive = false) sẽ không thể đăng nhập.
    /// </summary>
    /// <param name="id">Identity user ID.</param>
    /// <param name="isActive">true = mở khoá; false = khoá.</param>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserStatus(
        string id, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var success = await _userService.SetUserActiveStatusAsync(id, isActive, cancellationToken);
        return success
            ? NoContent()
            : NotFound(new { message = $"Không tìm thấy user có ID = {id}." });
    }

    // ------------------------------------------------------------------ //
    //  RESET PASSWORD
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Đặt lại mật khẩu cho user (Admin không cần biết mật khẩu cũ).
    /// </summary>
    /// <param name="id">Identity user ID.</param>
    /// <param name="request">Mật khẩu mới.</param>
    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        string id, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _userService.ResetPasswordAsync(id, request, cancellationToken);
            return success
                ? NoContent()
                : NotFound(new { message = $"Không tìm thấy user có ID = {id}." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ------------------------------------------------------------------ //
    //  DELETE
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Xóa vĩnh viễn tài khoản user (hard-delete).
    /// Hành động này không thể hoàn tác. Cân nhắc dùng khoá tài khoản thay thế.
    /// </summary>
    /// <param name="id">Identity user ID.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _userService.DeleteUserAsync(id, cancellationToken);
            return success
                ? NoContent()
                : NotFound(new { message = $"Không tìm thấy user có ID = {id}." });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
