using UrbanInfraSystem.Application.DTOs.Auth;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken);
}
