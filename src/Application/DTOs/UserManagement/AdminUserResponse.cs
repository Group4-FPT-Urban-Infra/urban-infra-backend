namespace UrbanInfraSystem.Application.DTOs.UserManagement;

/// <summary>
/// Thông tin user trả về cho Admin.
/// </summary>
public class AdminUserResponse
{
    /// <summary>Identity user ID (GUID dạng string).</summary>
    public string Id { get; set; } = default!;

    public string FullName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string? PhoneNumber { get; set; }

    /// <summary>Danh sách các role của user.</summary>
    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>Phòng ban phụ trách (chỉ DepartmentStaff mới có giá trị).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Tài khoản đang hoạt động hay đã bị khoá.</summary>
    public bool IsActive { get; set; }

    /// <summary>Thời điểm tạo tài khoản (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }
}
