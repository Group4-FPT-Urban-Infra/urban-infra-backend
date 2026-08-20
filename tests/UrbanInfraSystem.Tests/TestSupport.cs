using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Infrastructure.Hubs;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Tests;

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}

/// <summary>
/// Giả lập ICurrentUserService cho mục đích test.
/// Cho phép chỉ định UserId, Email, và Roles khi khởi tạo.
/// </summary>
internal sealed class TestCurrentUser : ICurrentUserService
{
    public string? UserId { get; }
    public string? Email { get; }
    public IReadOnlyList<string> Roles { get; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public TestCurrentUser(string? userId = "test-user-id", string? email = "test@example.com", IReadOnlyList<string>? roles = null)
    {
        UserId = userId;
        Email = email;
        Roles = roles ?? Array.Empty<string>();
    }

    public bool IsInRole(string role) => Roles.Contains(role);
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "UrbanInfraSystem.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.GetTempPath();
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>
/// Cung cấp ILogger rỗng (NullLogger) cho các service trong unit test.
/// </summary>
internal static class TestLogger
{
    public static ILogger<T> Create<T>() => NullLogger<T>.Instance;
}

/// <summary>
/// Tập hợp các phương thức factory để tạo mock object cho các dependency phức tạp.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Tạo mock cho UserManager<ApplicationUser> với một danh sách user có sẵn.
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(IList<ApplicationUser>? users = null)
    {
        users ??= new List<ApplicationUser>();
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        userManagerMock.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

        userManagerMock.Setup(x => x.Users).Returns(users.AsQueryable());

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => users.Add(user));

        userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) => users.FirstOrDefault(u => u.Id == userId));

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => users.FirstOrDefault(u => u.Email == email));

        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => users.FirstOrDefault(u => u.UserName == name));

        userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());
        userManagerMock.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
        userManagerMock.Setup(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.GetUsersInRoleAsync(It.IsAny<string>())).ReturnsAsync(users.ToList());

        return userManagerMock;
    }

    /// <summary>
    /// Tạo mock cho RoleManager<ApplicationRole> với một danh sách role có sẵn.
    /// </summary>
    public static Mock<RoleManager<ApplicationRole>> CreateRoleManagerMock(IList<ApplicationRole>? roles = null)
    {
        roles ??= new List<ApplicationRole>();
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(store.Object, null!, null!, null!, null!);

        roleManagerMock.Setup(x => x.Roles).Returns(roles.AsQueryable());
        roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string roleName) => roles.Any(r => r.Name == roleName));

        roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationRole>(role => roles.Add(role));

        roleManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string roleName) => roles.FirstOrDefault(r => r.Name == roleName));

        return roleManagerMock;
    }

    /// <summary>
    /// Tạo mock cho IJwtService.
    /// </summary>
    public static Mock<IJwtService> CreateJwtServiceMock()
    {
        var mock = new Mock<IJwtService>();
        mock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int?>()))
            .Returns(("fake-access-token-for-test", DateTime.UtcNow.AddMinutes(15)));
        mock.Setup(x => x.GenerateRefreshToken()).Returns("fake-refresh-token-for-test");
        return mock;
    }

    /// <summary>
    /// Tạo IOptions<JwtSettings> với các giá trị mặc định cho test.
    /// </summary>
    public static IOptions<JwtSettings> CreateJwtSettingsOptions()
    {
        var settings = new JwtSettings
        {
            Secret = "a-very-long-and-super-secret-key-for-testing-purposes-only-32-chars",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };
        return Options.Create(settings);
    }

    /// <summary>
    /// Tạo mock cho IHubContext<NotificationHub> để test các service có gửi thông báo SignalR.
    /// </summary>
    public static Mock<IHubContext<NotificationHub>> CreateNotificationHubMock()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

        return mockHubContext;
    }
}