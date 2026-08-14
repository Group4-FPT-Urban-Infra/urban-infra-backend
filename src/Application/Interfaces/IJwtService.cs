using System.Security.Claims;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IJwtService
{
    /// <summary>Sinh access token (JWT) chứa claims userId, email, roles.</summary>
    (string token, DateTime expiresAtUtc) GenerateAccessToken(string userId, string email, IEnumerable<string> roles, int? departmentId = null);

    /// <summary>Sinh chuỗi refresh token ngẫu nhiên (không phải JWT), lưu ở DB.</summary>
    string GenerateRefreshToken();

    /// <summary>Lấy ClaimsPrincipal từ access token đã hết hạn để phục vụ luồng refresh.</summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
