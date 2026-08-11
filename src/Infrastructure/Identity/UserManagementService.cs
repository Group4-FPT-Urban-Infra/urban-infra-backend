using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.UserManagement;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.Infrastructure.Identity;

/// <summary>
/// Triển khai nghiệp vụ quản lý User dành cho Admin.
/// Sử dụng ASP.NET Core Identity (UserManager, RoleManager) để thao tác tài khoản.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // ------------------------------------------------------------------ //
    //  GET LIST
    // ------------------------------------------------------------------ //

    public async Task<PagedResult<AdminUserResponse>> GetUsersAsync(
        AdminUserListRequest request, CancellationToken ct = default)
    {
        // Bắt đầu với toàn bộ user, sau đó lọc dần
        var query = _userManager.Users.AsNoTracking();

        // Lọc theo trạng thái hoạt động
        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        // Lọc theo phòng ban
        if (request.DepartmentId.HasValue)
            query = query.Where(u => u.DepartmentId == request.DepartmentId.Value);

        // Lọc theo từ khóa (FullName, Email, PhoneNumber)
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(kw) ||
                (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        // Lọc theo role: phải join sang Identity user-roles
        // UserManager không cung cấp IQueryable cho roles → lấy userId theo role trước
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!Roles.All.Contains(request.Role))
                throw new ArgumentException($"Role '{request.Role}' không hợp lệ. Các role hợp lệ: {string.Join(", ", Roles.All)}.");

            var usersInRole = await _userManager.GetUsersInRoleAsync(request.Role);
            var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => userIdsInRole.Contains(u.Id));
        }

        // Đếm tổng trước khi phân trang
        var totalCount = await query.CountAsync(ct);

        // Phân trang
        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        // Map sang DTO (cần lấy roles riêng vì Identity không join tự động)
        var items = new List<AdminUserResponse>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            items.Add(MapToResponse(u, roles));
        }

        return new PagedResult<AdminUserResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    // ------------------------------------------------------------------ //
    //  GET BY ID
    // ------------------------------------------------------------------ //

    public async Task<AdminUserResponse?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToResponse(user, roles);
    }

    // ------------------------------------------------------------------ //
    //  CREATE
    // ------------------------------------------------------------------ //

    public async Task<AdminUserResponse> CreateUserAsync(
        CreateUserByAdminRequest request, CancellationToken ct = default)
    {
        // Kiểm tra role hợp lệ
        if (!Roles.All.Contains(request.Role))
            throw new ArgumentException($"Role '{request.Role}' không hợp lệ. Các role hợp lệ: {string.Join(", ", Roles.All)}.");

        // Kiểm tra email chưa tồn tại
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new ArgumentException($"Email '{request.Email}' đã được sử dụng.");

        // Validate: DepartmentStaff phải có DepartmentId
        if (request.Role == Roles.DepartmentStaff && request.DepartmentId is null)
            throw new ArgumentException("DepartmentId là bắt buộc khi tạo tài khoản DepartmentStaff.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            DepartmentId = request.Role == Roles.DepartmentStaff ? request.DepartmentId : null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                "Không thể tạo tài khoản: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));

        // Đảm bảo role tồn tại rồi gán
        await EnsureRoleExistsAsync(request.Role);
        await _userManager.AddToRoleAsync(user, request.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToResponse(user, roles);
    }

    // ------------------------------------------------------------------ //
    //  UPDATE
    // ------------------------------------------------------------------ //

    public async Task<AdminUserResponse> UpdateUserAsync(
        string userId, UpdateUserByAdminRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"Không tìm thấy user có ID = {userId}.");

        if (!Roles.All.Contains(request.Role))
            throw new ArgumentException($"Role '{request.Role}' không hợp lệ. Các role hợp lệ: {string.Join(", ", Roles.All)}.");

        if (request.Role == Roles.DepartmentStaff && request.DepartmentId is null)
            throw new ArgumentException("DepartmentId là bắt buộc khi gán role DepartmentStaff.");

        // Cập nhật thông tin cơ bản
        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.DepartmentId = request.Role == Roles.DepartmentStaff ? request.DepartmentId : null;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(
                "Không thể cập nhật thông tin: " + string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        // Cập nhật role: xóa tất cả role cũ và gán role mới
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        await EnsureRoleExistsAsync(request.Role);
        await _userManager.AddToRoleAsync(user, request.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToResponse(user, roles);
    }

    // ------------------------------------------------------------------ //
    //  LOCK / UNLOCK
    // ------------------------------------------------------------------ //

    public async Task<bool> SetUserActiveStatusAsync(
        string userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    // ------------------------------------------------------------------ //
    //  RESET PASSWORD
    // ------------------------------------------------------------------ //

    public async Task<bool> ResetPasswordAsync(
        string userId, AdminResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        // Xóa password cũ và đặt password mới (admin flow, không cần OTP)
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            throw new InvalidOperationException(
                "Không thể xóa mật khẩu cũ: " + string.Join("; ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
            throw new InvalidOperationException(
                "Không thể đặt mật khẩu mới: " + string.Join("; ", addResult.Errors.Select(e => e.Description)));

        return true;
    }

    // ------------------------------------------------------------------ //
    //  DELETE
    // ------------------------------------------------------------------ //

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Không thể xóa tài khoản: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        return true;
    }

    // ------------------------------------------------------------------ //
    //  HELPERS
    // ------------------------------------------------------------------ //

    private static AdminUserResponse MapToResponse(ApplicationUser user, IList<string> roles)
        => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Roles = roles,
            DepartmentId = user.DepartmentId,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
    }
}
