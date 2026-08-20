using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using UrbanInfraSystem.Application.DTOs.Auth;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class AuthServiceTests
{
    private AppDbContext _context;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private Mock<IJwtService> _jwtServiceMock;
    private IOptions<JwtSettings> _jwtSettingsOptions;
    private AuthService _service;

    private List<ApplicationUser> _users;
    private List<ApplicationRole> _roles;

    public AuthServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        SeedData();

        _userManagerMock = TestHelpers.CreateUserManagerMock(_users);
        _roleManagerMock = TestHelpers.CreateRoleManagerMock(_roles);
        _jwtServiceMock = TestHelpers.CreateJwtServiceMock();
        _jwtSettingsOptions = TestHelpers.CreateJwtSettingsOptions();

        _service = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _jwtServiceMock.Object,
            _context,
            _jwtSettingsOptions
        );
    }

    private void SeedData()
    {
        _roles = new List<ApplicationRole>
        {
            new() { Name = Roles.Citizen, NormalizedName = Roles.Citizen.ToUpper() }
        };

        _users = new List<ApplicationUser>
        {
            new() { Id = "user-1", FullName = "Active User", Email = "active@test.com", UserName = "active@test.com", IsActive = true },
            new() { Id = "user-2", FullName = "Inactive User", Email = "inactive@test.com", UserName = "inactive@test.com", IsActive = false }
        };
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new.user@test.com",
            Password = "Password123!",
            FullName = "New User",
            PhoneNumber = "123456789"
        };

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(request.Email);

        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == request.Email), request.Password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Citizen), Times.Once);

        var refreshTokenInDb = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == result.RefreshToken);
        refreshTokenInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterRequest { Email = "active@test.com", Password = "p", FullName = "f" };

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email đã được sử dụng.");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndCreatesRefreshToken()
    {
        // Arrange
        var user = _users.First(u => u.Email == "active@test.com");
        var request = new LoginRequest { Email = user.Email!, Password = "password" };
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        var refreshTokenInDb = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        refreshTokenInDb.Should().NotBeNull();
        refreshTokenInDb!.Token.Should().Be(result.RefreshToken);

        var auditLogInDb = await _context.AuditLogs.FirstOrDefaultAsync(a => a.ActorUserId == user.Id && a.Action == "Login");
        auditLogInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailureResponse()
    {
        // Arrange
        var user = _users.First(u => u.Email == "active@test.com");
        var request = new LoginRequest { Email = user.Email!, Password = "wrong-password" };
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email hoặc mật khẩu không đúng.");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsFailureResponse()
    {
        // Arrange
        var user = _users.First(u => u.Email == "inactive@test.com");
        var request = new LoginRequest { Email = user.Email!, Password = "password" };

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email hoặc mật khẩu không đúng.");
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_IssuesNewTokensAndRevokesOld()
    {
        // Arrange
        var user = _users.First(u => u.Id == "user-1");
        var oldRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "old-refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        _context.RefreshTokens.Add(oldRefreshToken);
        await _context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id) }));
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);

        var request = new RefreshTokenRequest { AccessToken = "expired-access-token", RefreshToken = "old-refresh-token" };

        // Act
        var result = await _service.RefreshTokenAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RefreshToken.Should().NotBe("old-refresh-token");

        var oldTokenInDb = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "old-refresh-token");
        oldTokenInDb.RevokedAtUtc.Should().NotBeNull();
        oldTokenInDb.ReplacedByToken.Should().Be(result.RefreshToken);

        var newTokenInDb = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == result.RefreshToken);
        newTokenInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsFailureResponse()
    {
        // Arrange
        var user = _users.First(u => u.Id == "user-1");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id) }));
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);

        var request = new RefreshTokenRequest { AccessToken = "access-token", RefreshToken = "non-existent-refresh-token" };

        // Act
        var result = await _service.RefreshTokenAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Refresh token không hợp lệ hoặc đã hết hạn.");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ReturnsFailureResponse()
    {
        // Arrange
        var user = _users.First(u => u.Id == "user-1");
        var revokedToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = DateTime.UtcNow.AddDays(-1) // Already revoked
        };
        _context.RefreshTokens.Add(revokedToken);
        await _context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id) }));
        _jwtServiceMock.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);

        var request = new RefreshTokenRequest { AccessToken = "access-token", RefreshToken = "revoked-token" };

        // Act
        var result = await _service.RefreshTokenAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Refresh token không hợp lệ hoặc đã hết hạn.");
    }

    #endregion

    #region RevokeRefreshTokenAsync Tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithExistingActiveToken_RevokesTokenAndReturnsTrue()
    {
        // Arrange
        var user = _users.First(u => u.Id == "user-1");
        var tokenToRevoke = new RefreshToken
        {
            UserId = user.Id,
            Token = "token-to-revoke",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        _context.RefreshTokens.Add(tokenToRevoke);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RevokeRefreshTokenAsync(user.Id, "token-to-revoke");

        // Assert
        result.Should().BeTrue();
        var tokenInDb = await _context.RefreshTokens.FirstAsync(rt => rt.Token == "token-to-revoke");
        tokenInDb.RevokedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonExistentToken_ReturnsFalse()
    {
        // Arrange
        var userId = "user-1";

        // Act
        var result = await _service.RevokeRefreshTokenAsync(userId, "non-existent-token");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}