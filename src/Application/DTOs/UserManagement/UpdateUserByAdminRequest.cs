using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.UserManagement;

/// <summary>
/// Yêu cầu cập nhật thông tin user do Admin thực hiện.
/// </summary>
public class UpdateUserByAdminRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = default!;

    [Phone]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Role mới muốn gán cho user: "Admin" | "DepartmentStaff" | "Citizen".
    /// Service sẽ xóa toàn bộ role cũ và gán role mới.
    /// </summary>
    [Required]
    public string Role { get; set; } = default!;

    /// <summary>
    /// Phòng ban phụ trách mới — chỉ có ý nghĩa khi Role = "DepartmentStaff".
    /// Truyền null để xóa liên kết phòng ban.
    /// </summary>
    public Guid? DepartmentId { get; set; }
}
