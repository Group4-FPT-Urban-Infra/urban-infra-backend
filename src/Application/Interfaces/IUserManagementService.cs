using UrbanInfraSystem.Application.DTOs.UserManagement;

namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>
/// Giao diện quản lý User dành cho Admin.
/// Cho phép liệt kê, tạo, cập nhật, khoá/mở khoá, reset mật khẩu và xóa tài khoản người dùng.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Lấy danh sách user có phân trang và lọc theo role, trạng thái, từ khóa tìm kiếm.
    /// </summary>
    Task<PagedResult<AdminUserResponse>> GetUsersAsync(AdminUserListRequest request, CancellationToken ct = default);

    /// <summary>
    /// Lấy thông tin chi tiết một user theo ID.
    /// </summary>
    /// <returns>Null nếu không tìm thấy.</returns>
    Task<AdminUserResponse?> GetUserByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Tạo tài khoản mới với role chỉ định (Admin, DepartmentStaff, Citizen).
    /// Ném <see cref="ArgumentException"/> nếu email đã tồn tại hoặc role không hợp lệ.
    /// Ném <see cref="InvalidOperationException"/> nếu Identity trả về lỗi tạo user.
    /// </summary>
    Task<AdminUserResponse> CreateUserAsync(CreateUserByAdminRequest request, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật thông tin user: họ tên, SĐT, role, phòng ban.
    /// Ném <see cref="KeyNotFoundException"/> nếu user không tồn tại.
    /// Ném <see cref="ArgumentException"/> nếu role không hợp lệ.
    /// </summary>
    Task<AdminUserResponse> UpdateUserAsync(string userId, UpdateUserByAdminRequest request, CancellationToken ct = default);

    /// <summary>
    /// Khoá hoặc mở khoá tài khoản user (cập nhật IsActive).
    /// </summary>
    /// <returns>True nếu thành công; false nếu không tìm thấy user.</returns>
    Task<bool> SetUserActiveStatusAsync(string userId, bool isActive, CancellationToken ct = default);

    /// <summary>
    /// Đặt lại mật khẩu mới cho user (Admin-initiated, không cần mật khẩu cũ).
    /// </summary>
    /// <returns>True nếu thành công; false nếu không tìm thấy user.</returns>
    Task<bool> ResetPasswordAsync(string userId, AdminResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Xóa vĩnh viễn (hard-delete) tài khoản user.
    /// Ném <see cref="InvalidOperationException"/> nếu Identity trả về lỗi khi xóa.
    /// </summary>
    /// <returns>True nếu thành công; false nếu không tìm thấy user.</returns>
    Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default);
}
