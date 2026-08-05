using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UrbanInfraSystem.Application.Interfaces;

namespace UrbanInfraSystem.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    // JwtBearer được cấu hình MapInboundClaims = false nên claim giữ nguyên tên gốc
    // ("sub", "email") thay vì bị map sang ClaimTypes.* của .NET.
    public string? UserId => User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
