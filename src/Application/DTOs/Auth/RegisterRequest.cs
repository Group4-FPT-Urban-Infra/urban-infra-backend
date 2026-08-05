using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.Auth;

/// <summary>
/// Request đăng ký tài khoản. Mặc định role đăng ký công khai là Citizen;
/// Admin/DepartmentStaff được tạo bởi Admin qua API quản trị riêng (không public).
/// </summary>
public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string PhoneNumber { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;
}
