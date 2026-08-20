using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.UserManagement;

/// <summary>
/// Bộ lọc + phân trang cho endpoint lấy danh sách user (Admin).
/// </summary>
public class AdminUserListRequest
{
    /// <summary>Lọc theo tên Role: Admin | DepartmentStaff | Citizen. Bỏ trống = tất cả roles.</summary>
    public string? Role { get; set; }

    /// <summary>Lọc theo trạng thái hoạt động. null = tất cả.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Tìm kiếm theo FullName, Email hoặc PhoneNumber (contains, case-insensitive).</summary>
    public string? Keyword { get; set; }

    /// <summary>Lọc theo phòng ban (DepartmentId). Bỏ trống = tất cả.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Số trang hiện tại, bắt đầu từ 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Số phần tử mỗi trang, tối đa 100.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
