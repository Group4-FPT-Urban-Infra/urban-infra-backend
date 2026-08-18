using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.SlaPolicies;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class SlaPolicyServiceTests
{
    [Fact]
    public async Task CreateAsync_SuccessfullyCreatesSlaPolicy()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        await db.SaveChangesAsync();

        var service = new SlaPolicyService(db);
        var result = await service.CreateAsync(new CreateSlaPolicyRequest
        {
            IssueTypeId = 1,
            PriorityId = 1,
            FirstResponseMinutes = 30,
            ResolutionMinutes = 120
        });

        Assert.NotNull(result);
        Assert.Equal(1, result.IssueTypeId);
        Assert.Equal(1, result.PriorityId);
        Assert.Equal(30, result.FirstResponseMinutes);
        Assert.Equal(120, result.ResolutionMinutes);
        
        var policyInDb = await db.SlaPolicies.FirstOrDefaultAsync(p => p.Id == result.Id);
        Assert.NotNull(policyInDb);
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenPolicyAlreadyExists()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        db.SlaPolicies.Add(new SlaPolicy
        {
            IssueTypeId = 1,
            PriorityId = 1,
            FirstResponseMinutes = 30,
            ResolutionMinutes = 120
        });
        await db.SaveChangesAsync();

        var service = new SlaPolicyService(db);
        
        var request = new CreateSlaPolicyRequest
        {
            IssueTypeId = 1,
            PriorityId = 1,
            FirstResponseMinutes = 45,
            ResolutionMinutes = 90
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenFirstResponseGreaterThanResolution()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        await db.SaveChangesAsync();

        var service = new SlaPolicyService(db);
        
        var request = new CreateSlaPolicyRequest
        {
            IssueTypeId = 1,
            PriorityId = 1,
            FirstResponseMinutes = 120,
            ResolutionMinutes = 30
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
    }

    private static void SeedLookups(Infrastructure.Persistence.AppDbContext db)
    {
        db.IssueTypes.Add(new IssueType { IssueTypeId = 1, TypeCode = "T1", TypeName = "Type 1", IsActive = true });
        db.IssuePriorities.Add(new IssuePriority { PriorityId = 1, PriorityCode = "P1", PriorityName = "Priority 1", IsActive = true });
    }
}
