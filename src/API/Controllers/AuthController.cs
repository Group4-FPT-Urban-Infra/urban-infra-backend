using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrbanInfraSystem.Application.DTOs.Auth;
using UrbanInfraSystem.Application.DTOs.UserManagement;
using UrbanInfraSystem.Application.Interfaces;

namespace UrbanInfraSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserManagementService _userService;

    public AuthController(IAuthService authService, IUserManagementService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>Đăng ký tài khoản công dân (Citizen). Public, không yêu cầu đăng nhập.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Đăng nhập, trả về access token (JWT, ngắn hạn) + refresh token (dài hạn).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Cấp lại access token mới khi access token cũ hết hạn, dùng refresh token còn hiệu lực.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Đăng xuất: thu hồi refresh token hiện tại. Yêu cầu access token còn hợp lệ.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var revoked = await _authService.RevokeRefreshTokenAsync(userId, request.RefreshToken);
        return revoked ? NoContent() : BadRequest(new { message = "Refresh token không hợp lệ." });
    }

    /// <summary>Lấy thông tin người dùng hiện tại từ JWT token.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
        return user is null
            ? Unauthorized()
            : Ok(user);
    }

    /// <summary>
    /// Debug: Dump toàn bộ claims server đọc được từ JWT.
    /// Dùng để kiểm tra claim type và giá trị sau khi MapInboundClaims = false.
    /// </summary>
    [HttpGet("claims")]
    [Authorize]
    public IActionResult Claims()
    {
        var claims = User.Claims.Select(c => new { type = c.Type, value = c.Value });
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated,
            authType        = User.Identity?.AuthenticationType,
            claims
        });
    }
}
