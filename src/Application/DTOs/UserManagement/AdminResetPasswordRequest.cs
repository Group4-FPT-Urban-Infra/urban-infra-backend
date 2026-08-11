using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.UserManagement;

/// <summary>
/// Yêu cầu đặt lại mật khẩu cho user do Admin thực hiện (không cần mật khẩu cũ).
/// </summary>
public class AdminResetPasswordRequest
{
    [Required]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = default!;
}
