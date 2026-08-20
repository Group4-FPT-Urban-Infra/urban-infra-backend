using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueUpvoteTests
{
    [Fact]
    public async Task ToggleAsync_ShouldAddUpvoteAndIncrementCounter()
    {
        using var db = TestDb.Create();
        await SeedIssueAsync(db, 10, 0);
        var service = CreateService(db);

        var hasUpvoted = await service.ToggleAsync(10, "user-1");

        Assert.True(hasUpvoted);
        Assert.Equal(1, await db.IssueUpvotes.CountAsync(x => x.IssueId == 10));
        Assert.Equal(1, (await db.Issues.FindAsync(10L))!.UpvoteCount);
    }

    [Fact]
    public async Task ToggleAsync_ShouldRemoveExistingUpvoteAndDecrementCounter()
    {
        using var db = TestDb.Create();
        var issue = await SeedIssueAsync(db, 20, 1);
        db.IssueUpvotes.Add(new IssueUpvote { IssueId = issue.IssueId, UserId = "user-1" });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var hasUpvoted = await service.ToggleAsync(20, "user-1");

        Assert.False(hasUpvoted);
        Assert.Empty(await db.IssueUpvotes.Where(x => x.IssueId == 20).ToListAsync());
        Assert.Equal(0, (await db.Issues.FindAsync(20L))!.UpvoteCount);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveUpvoteAndNeverMakeCounterNegative()
    {
        using var db = TestDb.Create();
        var issue = await SeedIssueAsync(db, 30, 0);
        db.IssueUpvotes.Add(new IssueUpvote { IssueId = issue.IssueId, UserId = "user-1" });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.RemoveAsync(30, "user-1");

        Assert.Null(await db.IssueUpvotes.FindAsync(30L, "user-1"));
        Assert.Equal(0, (await db.Issues.FindAsync(30L))!.UpvoteCount);
    }

    [Fact]
    public async Task RemoveAsync_ShouldBeIdempotentWhenUserHasNotUpvoted()
    {
        using var db = TestDb.Create();
        await SeedIssueAsync(db, 40, 0);
        var service = CreateService(db);

        await service.RemoveAsync(40, "user-1");

        Assert.Empty(await db.IssueUpvotes.Where(x => x.IssueId == 40).ToListAsync());
        Assert.Equal(0, (await db.Issues.FindAsync(40L))!.UpvoteCount);
    }

    private static IssueUpvoteService CreateService(AppDbContext db) =>
        new(db, NullLogger<IssueUpvoteService>.Instance);

    private static async Task<Issue> SeedIssueAsync(AppDbContext db, long id, int upvoteCount)
    {
        var report = new Report
        {
            ReportId = id,
            PublicCode = $"REP-{id}",
            ReporterId = "reporter-1",
            Title = "Test report",
            Description = "Test description"
        };
        var issue = new Issue
        {
            IssueId = id,
            ReportId = report.ReportId,
            Report = report,
            PublicCode = $"ISS-{id}",
            ReporterId = "reporter-1",
            Title = "Test issue",
            Description = "Test description",
            UpvoteCount = upvoteCount
        };
        db.Issues.Add(issue);
        await db.SaveChangesAsync();
        return issue;
    }
}
