using Microsoft.AspNetCore.Identity;
using UrbanInfraSystem.Domain.Entities;

namespace UrbanInfraSystem.Infrastructure.Identity;

/// <summary>
/// Mở rộng IdentityUser mặc định để thêm các trường nghiệp vụ (họ tên, đơn vị công tác
/// cho Cán bộ xử lý, trạng thái hoạt động...).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = default!;


    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
