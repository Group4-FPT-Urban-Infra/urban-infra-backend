using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueUpvoteTests
{
    [Fact]
    public async Task UpvoteAsync_ShouldIncrementUpvoteCount_AndReturnHasUpvotedTrue()
    {
        // Arrange
        using var db = TestDb.Create();

        var report = new Report { ReportId = 1, PublicCode = "REP-1", ReporterId = "reporter-1", Title = "Đèn hỏng", Description = "Mô tả", UpvoteCount = 0 };
        db.Reports.Add(report);

        var issue = new Issue
        {
            IssueId = 10,
            PublicCode = "ISS-10",
            ReportId = 1,
            Report = report,
            ReporterId = "reporter-1",
            Title = "Đèn hỏng",
            Description = "Mô tả",
            UpvoteCount = 0
        };
        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser("citizen-user-1");
        var service = new IssueUpvoteService(db, currentUser);

        // Act
        var result = await service.UpvoteAsync(issueId: 10, userId: "citizen-user-1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.HasUpvoted);
        Assert.Equal(1, result.Data.UpvoteCount);

        // Verify Database
        var upvoteRecord = await db.ReportUpvotes.FirstOrDefaultAsync(u => u.ReportId == 1 && u.UserId == "citizen-user-1");
        Assert.NotNull(upvoteRecord);

        var updatedIssue = await db.Issues.Include(i => i.Report).FirstAsync(i => i.IssueId == 10);
        Assert.Equal(1, updatedIssue.UpvoteCount);
        Assert.Equal(1, updatedIssue.Report.UpvoteCount);
    }

    [Fact]
    public async Task UpvoteAsync_ShouldBeIdempotent_WhenUpvotingMultipleTimes()
    {
        // Arrange
        using var db = TestDb.Create();
        var report = new Report { ReportId = 2, PublicCode = "REP-2", ReporterId = "reporter-1", Title = "Cống tắc", Description = "Mô tả", UpvoteCount = 1 };
        db.Reports.Add(report);
        db.Issues.Add(new Issue { IssueId = 20, PublicCode = "ISS-20", ReportId = 2, Report = report, ReporterId = "reporter-1", Title = "Cống tắc", Description = "Mô tả", UpvoteCount = 1 });
        db.ReportUpvotes.Add(new ReportUpvote { ReportId = 2, UserId = "user-1" });
        await db.SaveChangesAsync();

        var service = new IssueUpvoteService(db, new TestCurrentUser("user-1"));

        // Act: Call upvote again for same user
        var result = await service.UpvoteAsync(20, "user-1");

        // Assert: Return current status, no duplicate entry created
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.HasUpvoted);
        Assert.Equal(1, result.Data.UpvoteCount);

        var countInDb = await db.ReportUpvotes.CountAsync(u => u.ReportId == 2);
        Assert.Equal(1, countInDb);
    }

    [Fact]
    public async Task UpvoteAsync_ShouldReturnFailure_WhenIssueNotFound()
    {
        // Arrange
        using var db = TestDb.Create();
        var service = new IssueUpvoteService(db, new TestCurrentUser());

        // Act
        var result = await service.UpvoteAsync(9999, "user-1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("9999", result.Message);
    }

    [Fact]
    public async Task RemoveUpvoteAsync_ShouldDecrementUpvoteCount_AndReturnHasUpvotedFalse()
    {
        // Arrange
        using var db = TestDb.Create();
        var report = new Report { ReportId = 3, PublicCode = "REP-3", ReporterId = "reporter-1", Title = "Rác thải", Description = "Mô tả", UpvoteCount = 1 };
        db.Reports.Add(report);
        db.Issues.Add(new Issue { IssueId = 30, PublicCode = "ISS-30", ReportId = 3, Report = report, ReporterId = "reporter-1", Title = "Rác thải", Description = "Mô tả", UpvoteCount = 1 });
        db.ReportUpvotes.Add(new ReportUpvote { ReportId = 3, UserId = "user-voter" });
        await db.SaveChangesAsync();

        var service = new IssueUpvoteService(db, new TestCurrentUser("user-voter"));

        // Act
        var result = await service.RemoveUpvoteAsync(30, "user-voter");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.HasUpvoted);
        Assert.Equal(0, result.Data.UpvoteCount);

        var upvoteInDb = await db.ReportUpvotes.FirstOrDefaultAsync(u => u.ReportId == 3 && u.UserId == "user-voter");
        Assert.Null(upvoteInDb);
    }

    [Fact]
    public async Task RemoveUpvoteAsync_ShouldBeIdempotent_WhenUserHasNotUpvoted()
    {
        // Arrange
        using var db = TestDb.Create();
        var report = new Report { ReportId = 4, PublicCode = "REP-4", ReporterId = "reporter-1", Title = "Cây đổ", Description = "Mô tả", UpvoteCount = 0 };
        db.Reports.Add(report);
        db.Issues.Add(new Issue { IssueId = 40, PublicCode = "ISS-40", ReportId = 4, Report = report, ReporterId = "reporter-1", Title = "Cây đổ", Description = "Mô tả", UpvoteCount = 0 });
        await db.SaveChangesAsync();

        var service = new IssueUpvoteService(db, new TestCurrentUser("user-never-upvoted"));

        // Act
        var result = await service.RemoveUpvoteAsync(40, "user-never-upvoted");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.HasUpvoted);
        Assert.Equal(0, result.Data.UpvoteCount);
    }

    [Fact]
    public async Task GetUpvoteStatusAsync_ShouldReturnCorrectUpvoteCountAndHasUpvoted()
    {
        // Arrange
        using var db = TestDb.Create();
        var report = new Report { ReportId = 5, PublicCode = "REP-5", ReporterId = "reporter-1", Title = "Đèn đường", Description = "Mô tả", UpvoteCount = 2 };
        db.Reports.Add(report);
        db.Issues.Add(new Issue { IssueId = 50, PublicCode = "ISS-50", ReportId = 5, Report = report, ReporterId = "reporter-1", Title = "Đèn đường", Description = "Mô tả", UpvoteCount = 2 });
        db.ReportUpvotes.Add(new ReportUpvote { ReportId = 5, UserId = "user-1" });
        db.ReportUpvotes.Add(new ReportUpvote { ReportId = 5, UserId = "user-2" });
        await db.SaveChangesAsync();

        var service = new IssueUpvoteService(db, new TestCurrentUser());

        // Act: User 1
        var status1 = await service.GetUpvoteStatusAsync(50, "user-1");
        Assert.True(status1.Success);
        Assert.True(status1.Data!.HasUpvoted);
        Assert.Equal(2, status1.Data.UpvoteCount);

        // Act: User 3 (not upvoted)
        var status3 = await service.GetUpvoteStatusAsync(50, "user-3");
        Assert.True(status3.Success);
        Assert.False(status3.Data!.HasUpvoted);
        Assert.Equal(2, status3.Data.UpvoteCount);

        // Act: Anonymous (userId null)
        var statusAnon = await service.GetUpvoteStatusAsync(50, null);
        Assert.True(statusAnon.Success);
        Assert.False(statusAnon.Data!.HasUpvoted);
        Assert.Equal(2, statusAnon.Data.UpvoteCount);
    }
}
