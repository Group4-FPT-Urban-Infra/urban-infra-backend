using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueUpdateTimelineTests
{
    [Fact]
    public async Task GetIssueTimelineAsync_ShouldReturnTimelineItems_OrderedByCreatedAtAscending()
    {
        // Arrange
        using var db = TestDb.Create();
        var env = new TestWebHostEnvironment();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IssueService>();

        // Seed Statuses
        var s1 = new IssueStatus { StatusId = 1, StatusCode = "NEW", StatusName = "Mới tiếp nhận", DisplayOrder = 1, IsActive = true };
        var s2 = new IssueStatus { StatusId = 2, StatusCode = "IN_PROGRESS", StatusName = "Đang xử lý", DisplayOrder = 2, IsActive = true };
        var s3 = new IssueStatus { StatusId = 3, StatusCode = "RESOLVED", StatusName = "Đã xử lý", DisplayOrder = 3, IsActive = true };
        db.IssueStatuses.AddRange(s1, s2, s3);

        // Seed Issue
        var issue = new Issue
        {
            IssueId = 601,
            PublicCode = "ISS-601",
            ReporterId = "reporter-1",
            StatusId = 3,
            Title = "Hố ga mất nắp",
            Description = "Nguy hiểm cho người tham gia giao thông",
            ReportedAt = DateTime.UtcNow.AddDays(-3)
        };
        db.Issues.Add(issue);

        // Seed Updates
        db.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = 601,
            CreatedBy = "system",
            FromStatusId = null,
            ToStatusId = 1,
            Note = "Báo cáo được ghi nhận vào hệ thống.",
            IsSystemGenerated = true,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });

        db.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = 601,
            CreatedBy = "staff-user-1",
            FromStatusId = 1,
            ToStatusId = 2,
            Note = "Cán bộ đã tiếp nhận và đang tiến hành thay nắp hố ga mới.",
            IsSystemGenerated = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });

        db.IssueUpdates.Add(new IssueUpdate
        {
            IssueId = 601,
            CreatedBy = "staff-user-1",
            FromStatusId = 2,
            ToStatusId = 3,
            Note = "Đã hoàn thành lắp nắp hố ga mới.",
            IsSystemGenerated = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        await db.SaveChangesAsync();

        var issueService = new IssueService(db, env, logger);

        // Act
        var result = await issueService.GetIssueTimelineAsync(601);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Count);

        // First timeline item
        Assert.NotNull(result.Data[0].ToStatus);
        Assert.Equal(1, result.Data[0].ToStatus!.Id);
        Assert.Equal("Mới tiếp nhận", result.Data[0].ToStatus!.Name);
        Assert.True(result.Data[0].IsSystemGenerated);

        // Second timeline item (status transition NEW -> IN_PROGRESS)
        Assert.NotNull(result.Data[1].ToStatus);
        Assert.Equal(1, result.Data[1].FromStatus?.Id);
        Assert.Equal(2, result.Data[1].ToStatus!.Id);
        Assert.Equal("Đang xử lý", result.Data[1].ToStatus!.Name);

        // Third timeline item (status transition IN_PROGRESS -> RESOLVED)
        Assert.NotNull(result.Data[2].ToStatus);
        Assert.Equal(2, result.Data[2].FromStatus?.Id);
        Assert.Equal(3, result.Data[2].ToStatus!.Id);
        Assert.Equal("Đã xử lý", result.Data[2].ToStatus!.Name);
    }

    [Fact]
    public async Task GetIssueTimelineAsync_ShouldReturnNotFound_WhenIssueDoesNotExist()
    {
        // Arrange
        using var db = TestDb.Create();
        var env = new TestWebHostEnvironment();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IssueService>();
        var issueService = new IssueService(db, env, logger);

        // Act
        var result = await issueService.GetIssueTimelineAsync(9999);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("9999", result.Message);
    }
}
