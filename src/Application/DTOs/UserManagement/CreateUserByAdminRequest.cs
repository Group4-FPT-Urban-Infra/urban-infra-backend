using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.UserManagement;

/// <summary>
/// Yêu cầu tạo user mới do Admin khởi tạo.
/// Admin có thể chỉ định role (bao gồm cả Admin / DepartmentStaff) —
/// khác với public RegisterAsync chỉ tạo Citizen.
/// </summary>
public class CreateUserByAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = default!;

    [Phone]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Role được gán: "Admin" | "DepartmentStaff" | "Citizen".
    /// Mặc định là "Citizen" nếu không truyền.
    /// </summary>
    [Required]
    public string Role { get; set; } = "Citizen";

    /// <summary>
    /// Bắt buộc nếu Role = "DepartmentStaff". Bỏ qua với các role khác.
    /// </summary>
    public int? DepartmentId { get; set; }
}
