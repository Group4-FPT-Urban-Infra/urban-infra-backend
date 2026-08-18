using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Tests;

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}

internal sealed class TestCurrentUser(string userId = "admin-test") : ICurrentUserService
{
    public string? UserId => userId;
    public string? Email => "admin@test.local";
    public IReadOnlyList<string> Roles => ["Admin"];
    public bool IsAuthenticated => true;
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
