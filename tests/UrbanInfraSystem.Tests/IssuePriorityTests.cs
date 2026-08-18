using UrbanInfraSystem.Application.DTOs.IssuePriorities;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssuePriorityTests
{
    [Fact]
    public async Task CreateAsync_NormalizesInput()
    {
        await using var db = TestDb.Create();

        var result = await new IssuePriorityService(db).CreateAsync(new CreateIssuePriorityRequest
        {
            PriorityCode = " critical ", PriorityName = " Khẩn cấp ", SeverityRank = 4
        });

        Assert.Equal("CRITICAL", result.PriorityCode);
        Assert.Equal("Khẩn cấp", result.PriorityName);
        Assert.Equal(4, result.SeverityRank);
    }

    [Theory]
    [InlineData("HIGH", 4, "Mã mức ưu tiên")]
    [InlineData("OTHER", 3, "Thứ hạng mức độ")]
    public async Task CreateAsync_RejectsDuplicateCodeOrRank(
        string code, byte rank, string expectedMessage)
    {
        await using var db = TestDb.Create();
        db.IssuePriorities.Add(new IssuePriority
        {
            PriorityId = 1, PriorityCode = "HIGH", PriorityName = "Cao", SeverityRank = 3
        });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IssuePriorityService(db).CreateAsync(new CreateIssuePriorityRequest
            {
                PriorityCode = code, PriorityName = "Mới", SeverityRank = rank
            }));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task GetActiveAsync_FiltersInactiveAndSortsBySeverity()
    {
        await using var db = TestDb.Create();
        db.IssuePriorities.AddRange(
            new IssuePriority { PriorityId = 1, PriorityCode = "HIGH", PriorityName = "Cao", SeverityRank = 3 },
            new IssuePriority { PriorityId = 2, PriorityCode = "LOW", PriorityName = "Thấp", SeverityRank = 1 },
            new IssuePriority { PriorityId = 3, PriorityCode = "OLD", PriorityName = "Cũ", SeverityRank = 2, IsActive = false });
        await db.SaveChangesAsync();

        var results = await new IssuePriorityService(db).GetActiveAsync();

        Assert.Equal(["LOW", "HIGH"], results.Select(x => x.PriorityCode));
    }

    [Fact]
    public async Task UpdateDeactivateAndDeleteAsync_PersistChanges()
    {
        await using var db = TestDb.Create();
        db.IssuePriorities.Add(new IssuePriority
        {
            PriorityId = 7, PriorityCode = "HIGH", PriorityName = "Cao", SeverityRank = 3
        });
        await db.SaveChangesAsync();
        var service = new IssuePriorityService(db);

        var updated = await service.UpdateAsync(7, new UpdateIssuePriorityRequest
        {
            PriorityName = "Rất cao", SeverityRank = 4
        });
        var deactivated = await service.DeactivateAsync(7);
        var deleted = await service.DeleteAsync(7);

        Assert.Equal("Rất cao", updated!.PriorityName);
        Assert.Equal(4, updated.SeverityRank);
        Assert.True(deactivated);
        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(7));
    }
}
