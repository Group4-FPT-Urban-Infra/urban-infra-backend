namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>
/// Truy xuất thông tin user hiện tại từ HttpContext (dùng để ghi CreatedBy/UpdatedBy
/// phục vụ yêu cầu "ghi log đầy đủ các bước xử lý" ở mục V).
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
