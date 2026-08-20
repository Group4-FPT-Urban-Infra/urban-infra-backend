using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueReportServiceTests
{
    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturnCorrectActiveAndResolvedCounts()
    {
        // Arrange
        using var db = TestDb.Create();
        var env = new TestWebHostEnvironment();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IssueService>();

        // Seed Roles
        var citizenRole = new ApplicationRole { Id = "role-citizen", Name = Roles.Citizen };
        db.Roles.Add(citizenRole);

        // Seed Citizen User
        var citizenUser = new ApplicationUser { Id = "citizen-1", UserName = "citizen1@test.com", Email = "citizen1@test.com", FullName = "Citizen Test" };
        db.Users.Add(citizenUser);
        db.UserRoles.Add(new IdentityUserRole<string> { RoleId = "role-citizen", UserId = "citizen-1" });

        // Seed Statuses
        var sNew = new IssueStatus { StatusId = 1, StatusCode = "NEW", StatusName = "Mới tiếp nhận", DisplayOrder = 1, IsActive = true };
        var sResolved = new IssueStatus { StatusId = 2, StatusCode = "RESOLVED", StatusName = "Đã xử lý", DisplayOrder = 2, IsActive = true };
        db.IssueStatuses.AddRange(sNew, sResolved);

        // Seed Issues
        // 1 active public issue
        db.Issues.Add(new Issue { IssueId = 701, PublicCode = "ISS-701", ReporterId = "citizen-1", StatusId = 1, Status = sNew, IsPublic = true, Title = "Active 1", Description = "Active 1", ReportedAt = DateTime.UtcNow });
        // 1 resolved this week
        db.Issues.Add(new Issue { IssueId = 702, PublicCode = "ISS-702", ReporterId = "citizen-1", StatusId = 2, Status = sResolved, IsPublic = true, Title = "Resolved 1", Description = "Resolved 1", ReportedAt = DateTime.UtcNow, ResolvedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var service = new IssueService(db, env, logger);

        // Act
        var result = await service.GetDashboardStatsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.ActiveIssuesCount);
        Assert.Equal(1, result.Data.ResolvedThisWeekCount);
        Assert.Equal(1, result.Data.CitizenUsersCount);
    }

    [Fact]
    public async Task GetMyIssuesAsync_ShouldReturnPagedReportsByReporterId()
    {
        // Arrange
        using var db = TestDb.Create();
        var env = new TestWebHostEnvironment();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IssueService>();

        var reporterId = "citizen-reporter-10";

        var area = new Area { AreaId = 1, AreaCode = "DIST_1", AreaName = "Quận 1", AreaType = "District", IsActive = true };
        var type = new IssueType { IssueTypeId = 1, TypeCode = "LIGHT", TypeName = "Đèn chiếu sáng", IsActive = true };
        var priority = new IssuePriority { PriorityId = 1, PriorityCode = "MEDIUM", PriorityName = "Trung bình", SeverityRank = 2, IsActive = true };
        var status = new IssueStatus { StatusId = 1, StatusCode = "NEW", StatusName = "Mới tiếp nhận", IsActive = true };

        db.Areas.Add(area);
        db.IssueTypes.Add(type);
        db.IssuePriorities.Add(priority);
        db.IssueStatuses.Add(status);

        db.Issues.Add(new Issue
        {
            IssueId = 801,
            PublicCode = "ISS-801",
            ReporterId = reporterId,
            AreaId = 1,
            Area = area,
            IssueTypeId = 1,
            IssueType = type,
            PriorityId = 1,
            Priority = priority,
            StatusId = 1,
            Status = status,
            Title = "Báo cáo của tui 1",
            Description = "Mô tả báo cáo",
            ReportedAt = DateTime.UtcNow.AddHours(-5)
        });

        db.Issues.Add(new Issue
        {
            IssueId = 802,
            PublicCode = "ISS-802",
            ReporterId = "other-citizen",
            AreaId = 1,
            Area = area,
            IssueTypeId = 1,
            IssueType = type,
            PriorityId = 1,
            Priority = priority,
            StatusId = 1,
            Status = status,
            Title = "Báo cáo của người khác",
            Description = "Mô tả khác",
            ReportedAt = DateTime.UtcNow.AddHours(-1)
        });

        await db.SaveChangesAsync();

        var service = new IssueService(db, env, logger);
        var request = new GetMyIssuesRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await service.GetMyIssuesAsync(request, reporterId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalItems);
        Assert.Single(result.Data.Items);
        Assert.Equal(801, result.Data.Items[0].Id);
        Assert.Equal("Báo cáo của tui 1", result.Data.Items[0].Title);
    }

    [Fact]
    public async Task FindNearbyIssuesAsync_ShouldReturnIssuesWithinRadius()
    {
        // Arrange
        using var db = TestDb.Create();
        var env = new TestWebHostEnvironment();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IssueService>();

        var area = new Area { AreaId = 1, AreaCode = "A1", AreaName = "Khu vực 1", AreaType = "District", IsActive = true };
        var type = new IssueType { IssueTypeId = 1, TypeCode = "ROAD", TypeName = "Hạ tầng đường", IsActive = true };
        var priority = new IssuePriority { PriorityId = 1, PriorityCode = "MEDIUM", PriorityName = "Trung bình", SeverityRank = 2, IsActive = true };
        var status = new IssueStatus { StatusId = 1, StatusCode = "NEW", StatusName = "Mới tiếp nhận", IsActive = true };

        db.Areas.Add(area);
        db.IssueTypes.Add(type);
        db.IssuePriorities.Add(priority);
        db.IssueStatuses.Add(status);

        // Point near (20.46, 106.138)
        db.Issues.Add(new Issue
        {
            IssueId = 901,
            PublicCode = "ISS-901",
            ReporterId = "c1",
            AreaId = 1,
            Area = area,
            IssueTypeId = 1,
            IssueType = type,
            PriorityId = 1,
            Priority = priority,
            StatusId = 1,
            Status = status,
            Latitude = 20.4601m,
            Longitude = 106.1381m,
            IsPublic = true,
            Title = "Gần vị trí test",
            Description = "Mô tả vị trí",
            ReportedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new IssueService(db, env, logger);
        var request = new FindNearbyIssuesRequest
        {
            Latitude = 20.46m,
            Longitude = 106.138m,
            RadiusMeters = 5000,
            IssueTypeId = 1
        };

        // Act
        var result = await service.FindNearbyIssuesAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(901, result.Data[0].Id);
        Assert.Equal("Gần vị trí test", result.Data[0].Title);
    }

    [Fact]
    public async Task SearchIssuesAsync_ShouldFilterByKeywordAndArea()
    {
        // Arrange
        using var db = TestDb.Create();
        var env = new TestWebHostEnvironment();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IssueService>();

        var area1 = new Area { AreaId = 1, AreaCode = "A1", AreaName = "Quận Ba Đình", AreaType = "District", IsActive = true };
        var area2 = new Area { AreaId = 2, AreaCode = "A2", AreaName = "Quận Hoàn Kiếm", AreaType = "District", IsActive = true };
        var type = new IssueType { IssueTypeId = 1, TypeCode = "DRAIN", TypeName = "Cống rãnh", IsActive = true };
        var priority = new IssuePriority { PriorityId = 1, PriorityCode = "MEDIUM", PriorityName = "Trung bình", SeverityRank = 2, IsActive = true };
        var status = new IssueStatus { StatusId = 1, StatusCode = "NEW", StatusName = "Mới tiếp nhận", IsActive = true };

        db.Areas.AddRange(area1, area2);
        db.IssueTypes.Add(type);
        db.IssuePriorities.Add(priority);
        db.IssueStatuses.Add(status);

        db.Issues.Add(new Issue
        {
            IssueId = 1001,
            PublicCode = "ISS-1001",
            ReporterId = "c1",
            AreaId = 1,
            Area = area1,
            IssueTypeId = 1,
            IssueType = type,
            PriorityId = 1,
            Priority = priority,
            StatusId = 1,
            Status = status,
            Title = "Cống tắc đường Đội Cấn",
            Description = "Nước tràn ra đường",
            IsPublic = true,
            ReportedAt = DateTime.UtcNow
        });

        db.Issues.Add(new Issue
        {
            IssueId = 1002,
            PublicCode = "ISS-1002",
            ReporterId = "c2",
            AreaId = 2,
            Area = area2,
            IssueTypeId = 1,
            IssueType = type,
            PriorityId = 1,
            Priority = priority,
            StatusId = 1,
            Status = status,
            Title = "Đèn hỏng Tràng Tiền",
            Description = "Bóng bị hỏng",
            IsPublic = true,
            ReportedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new IssueService(db, env, logger);

        // Act: Filter by Keyword "Đội Cấn"
        var request = new SearchIssuesRequest { Keyword = "Đội Cấn", AreaId = 1, Page = 1, PageSize = 10 };
        var result = await service.SearchIssuesAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalItems);
        Assert.Equal(1001, result.Data.Items[0].Id);
    }
}
