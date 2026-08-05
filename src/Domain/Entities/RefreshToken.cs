namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Lưu refresh token để cấp lại access token mà không cần đăng nhập lại.
/// Mỗi user có thể có nhiều refresh token (đăng nhập từ nhiều thiết bị).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = default!;

    public string Token { get; set; } = default!;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
}
