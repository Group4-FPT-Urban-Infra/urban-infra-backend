using UrbanInfraSystem.Application.DTOs.IssueStatuses;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueStatusTests
{
    [Fact]
    public async Task CreateAsync_NormalizesCodeAndTrimsName()
    {
        await using var db = TestDb.Create();
        var service = new IssueStatusService(db);

        var result = await service.CreateAsync(new CreateIssueStatusRequest
        {
            StatusCode = " in_progress ",
            StatusName = " Đang xử lý ",
            DisplayOrder = 2
        });

        Assert.Equal("IN_PROGRESS", result.StatusCode);
        Assert.Equal("Đang xử lý", result.StatusName);
        Assert.Equal(2, result.DisplayOrder);
        Assert.True(result.IsActive);
    }

    [Theory]
    [InlineData("OPEN", 2, "Mã trạng thái")]
    [InlineData("OTHER", 1, "Thứ tự hiển thị")]
    public async Task CreateAsync_RejectsDuplicateCodeOrDisplayOrder(
        string code, short order, string expectedMessage)
    {
        await using var db = TestDb.Create();
        db.IssueStatuses.Add(new IssueStatus
        {
            StatusId = 1, StatusCode = "OPEN", StatusName = "Mở", DisplayOrder = 1
        });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IssueStatusService(db).CreateAsync(new CreateIssueStatusRequest
            {
                StatusCode = code, StatusName = "Mới", DisplayOrder = order
            }));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task GetActiveAsync_FiltersInactiveAndSortsByDisplayOrder()
    {
        await using var db = TestDb.Create();
        db.IssueStatuses.AddRange(
            new IssueStatus { StatusId = 1, StatusCode = "DONE", StatusName = "Xong", DisplayOrder = 3 },
            new IssueStatus { StatusId = 2, StatusCode = "OPEN", StatusName = "Mở", DisplayOrder = 1 },
            new IssueStatus { StatusId = 3, StatusCode = "OLD", StatusName = "Cũ", DisplayOrder = 2, IsActive = false });
        await db.SaveChangesAsync();

        var results = await new IssueStatusService(db).GetActiveAsync();

        Assert.Equal(["OPEN", "DONE"], results.Select(x => x.StatusCode));
    }

    [Fact]
    public async Task UpdateDeactivateAndDeleteAsync_PersistChanges()
    {
        await using var db = TestDb.Create();
        db.IssueStatuses.Add(new IssueStatus
        {
            StatusId = 5, StatusCode = "OPEN", StatusName = "Mở", DisplayOrder = 1
        });
        await db.SaveChangesAsync();
        var service = new IssueStatusService(db);

        var updated = await service.UpdateAsync(5, new UpdateIssueStatusRequest
        {
            StatusName = "Đang tiếp nhận", IsPublicVisible = false
        });
        var deactivated = await service.DeactivateAsync(5);
        var deleted = await service.DeleteAsync(5);

        Assert.Equal("Đang tiếp nhận", updated!.StatusName);
        Assert.False(updated.IsPublicVisible);
        Assert.True(deactivated);
        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(5));
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_RejectsClosed_WhenIssueWasNotResolved()
    {
        await using var db = TestDb.Create();
        IssueTests.SeedLookups(db);
        db.IssueStatuses.Add(new IssueStatus
        {
            StatusId = 3,
            StatusCode = "CLOSED",
            StatusName = "Đã đóng",
            IsClosed = true,
            DisplayOrder = 3
        });
        db.Issues.Add(IssueTests.NewIssue(30, "ISS-030", "Chưa giải quyết"));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DepartmentManagerIssueService(db).UpdateIssueStatusAsync(
                30,
                new DepartmentManagerUpdateIssueStatusRequest { StatusId = 3 },
                "manager-test"));

        Assert.Contains("RESOLVED", exception.Message);
        Assert.Equal(1, (await db.Issues.FindAsync(30L))!.StatusId);
        Assert.Empty(db.IssueUpdates);
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_AllowsClosed_AfterIssueWasResolved()
    {
        await using var db = TestDb.Create();
        IssueTests.SeedLookups(db);
        db.IssueStatuses.AddRange(
            new IssueStatus
            {
                StatusId = 2, StatusCode = "RESOLVED", StatusName = "Đã giải quyết", DisplayOrder = 2
            },
            new IssueStatus
            {
                StatusId = 3, StatusCode = "CLOSED", StatusName = "Đã đóng", IsClosed = true, DisplayOrder = 3
            });
        var issue = IssueTests.NewIssue(31, "ISS-031", "Đã giải quyết");
        issue.StatusId = 2;
        issue.ResolvedAt = DateTime.UtcNow.AddMinutes(-10);
        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var result = await new DepartmentManagerIssueService(db).UpdateIssueStatusAsync(
            31,
            new DepartmentManagerUpdateIssueStatusRequest { StatusId = 3, Note = "Đóng sự cố" },
            "manager-test");

        var persisted = (await db.Issues.FindAsync(31L))!;
        Assert.Equal("Đã đóng", result.StatusName);
        Assert.Equal(3, persisted.StatusId);
        Assert.NotNull(persisted.ClosedAt);
        Assert.Single(db.IssueUpdates);
    }
}
