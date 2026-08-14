using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UrbanInfraSystem.Application.DTOs.Auth;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Identity;

/// <summary>
/// Xử lý nghiệp vụ xác thực: đăng ký (mặc định role Citizen), đăng nhập,
/// cấp/làm mới access+refresh token, thu hồi token khi logout.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtService jwtService,
        AppDbContext db,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _db = db;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            return new AuthResponse { Success = false, Message = "Email đã được sử dụng." };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Success = false,
                Message = string.Join("; ", result.Errors.Select(e => e.Description))
            };
        }

        // Người dân tự đăng ký công khai luôn được gán role Citizen.
        // Admin / DepartmentStaff được tạo bởi Admin qua API quản trị riêng.
        if (!await _roleManager.RoleExistsAsync(Roles.Citizen))
        {
            await _roleManager.CreateAsync(new ApplicationRole { Name = Roles.Citizen });
        }
        await _userManager.AddToRoleAsync(user, Roles.Citizen);

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return new AuthResponse { Success = false, Message = "Email hoặc mật khẩu không đúng." };
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            return new AuthResponse { Success = false, Message = "Email hoặc mật khẩu không đúng." };
        }

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (userId == null)
        {
            return new AuthResponse { Success = false, Message = "Access token không hợp lệ." };
        }

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

        if (storedToken == null || !storedToken.IsActive)
        {
            return new AuthResponse { Success = false, Message = "Refresh token không hợp lệ hoặc đã hết hạn." };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return new AuthResponse { Success = false, Message = "Tài khoản không tồn tại hoặc đã bị khoá." };
        }

        // Thu hồi token cũ, cấp token mới (rotation) để hạn chế rủi ro replay.
        storedToken.RevokedAtUtc = DateTime.UtcNow;

        var response = await IssueTokensAsync(user);
        storedToken.ReplacedByToken = response.RefreshToken;
        await _db.SaveChangesAsync();

        return response;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (storedToken == null || !storedToken.IsActive)
        {
            return false;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var deptMembership = await _db.DepartmentMembers
            .FirstOrDefaultAsync(dm => dm.UserId == user.Id && dm.IsActive);

        var (accessToken, expiresAtUtc) = _jwtService.GenerateAccessToken(
            user.Id, user.Email!, roles, deptMembership?.DepartmentId);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        });
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAtUtc = expiresAtUtc,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles
            }
        };
    }
}
