using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.RoutingRules;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class RoutingRuleServiceTests
{
    [Fact]
    public async Task CreateAsync_SuccessfullyCreatesRoutingRule()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        await db.SaveChangesAsync();

        var service = new RoutingRuleService(db);
        var result = await service.CreateAsync(new CreateRoutingRuleRequest
        {
            IssueTypeId = 1,
            AreaId = 1,
            DepartmentId = 1,
            IsActive = true
        });

        Assert.NotNull(result);
        Assert.Equal(1, result.IssueTypeId);
        Assert.Equal(1, result.AreaId);
        Assert.Equal(1, result.DepartmentId);
        
        var ruleInDb = await db.RoutingRules.FirstOrDefaultAsync(r => r.RoutingRuleId == result.RoutingRuleId);
        Assert.NotNull(ruleInDb);
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenRuleAlreadyExists()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        db.RoutingRules.Add(new RoutingRule
        {
            IssueTypeId = 1,
            AreaId = 1,
            DepartmentId = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new RoutingRuleService(db);
        
        var request = new CreateRoutingRuleRequest
        {
            IssueTypeId = 1,
            AreaId = 1,
            DepartmentId = 2,
            IsActive = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task ResolveAsync_ReturnsCorrectRule()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        db.RoutingRules.Add(new RoutingRule
        {
            IssueTypeId = 1,
            AreaId = 1,
            DepartmentId = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new RoutingRuleService(db);
        var result = await service.ResolveAsync(1, 1);

        Assert.NotNull(result);
        Assert.Equal(1, result.DepartmentId);
    }

    private static void SeedLookups(Infrastructure.Persistence.AppDbContext db)
    {
        db.IssueTypes.Add(new IssueType { IssueTypeId = 1, TypeCode = "T1", TypeName = "Type 1", IsActive = true });
        db.Areas.Add(new Area { AreaId = 1, AreaCode = "A1", AreaName = "Area 1", AreaType = "CITY", IsActive = true });
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Dept 1", IsActive = true });
        db.Departments.Add(new Department { DepartmentId = 2, DepartmentCode = "D2", DepartmentName = "Dept 2", IsActive = true });
    }
}
