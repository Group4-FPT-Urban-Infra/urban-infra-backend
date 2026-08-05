using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.API.Controllers;

/// <summary>
/// Controller mẫu để kiểm tra nhanh phân quyền theo Role sau khi có JWT.
/// Xoá/khác đi khi bắt đầu code các module nghiệp vụ thật ở Sprint 2.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    [HttpGet("admin-only")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult AdminOnly() => Ok(new { message = "Bạn là Quản trị viên." });

    [HttpGet("staff-only")]
    [Authorize(Roles = Roles.DepartmentStaff)]
    public IActionResult StaffOnly() => Ok(new { message = "Bạn là Cán bộ xử lý." });

    [HttpGet("citizen-only")]
    [Authorize(Roles = Roles.Citizen)]
    public IActionResult CitizenOnly() => Ok(new { message = "Bạn là Công dân." });

    [HttpGet("any-authenticated-user")]
    [Authorize]
    public IActionResult AnyUser() => Ok(new { message = "Token hợp lệ, đã xác thực." });
}
