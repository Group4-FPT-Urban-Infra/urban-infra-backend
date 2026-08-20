using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using UrbanInfraSystem.Application.DTOs.EscalationRules;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class EscalationRuleServiceTests
{
    private static Mock<RoleManager<ApplicationRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(store.Object, null, null, null, null);
        roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        return roleManagerMock;
    }

    [Fact]
    public async Task CreateAsync_SuccessfullyCreatesEscalationRule()
    {
        await using var db = TestDb.Create();
        var slaId = Guid.NewGuid();
        db.SlaPolicies.Add(new SlaPolicy { Id = slaId, IssueTypeId = 1, PriorityId = 1, FirstResponseMinutes = 30, ResolutionMinutes = 120 });
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Dept 1", IsActive = true });
        await db.SaveChangesAsync();

        var roleManagerMock = CreateRoleManagerMock();
        var service = new EscalationRuleService(db, roleManagerMock.Object);

        var request = new CreateEscalationRuleRequest
        {
            SlaPolicyId = slaId,
            OverdueMinutes = 30,
            EscalationLevel = 1,
            TargetDepartmentId = 1,
            TargetRoleName = "Admin",
            IsActive = true
        };

        var result = await service.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(slaId, result.SlaPolicyId);
        Assert.Equal(30, result.OverdueMinutes);
        Assert.Equal(1, result.EscalationLevel);
        Assert.Equal(1, result.TargetDepartmentId);
        Assert.Equal("Admin", result.TargetRoleName);

        var ruleInDb = await db.EscalationRules.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(ruleInDb);
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenSlaPolicyNotFound()
    {
        await using var db = TestDb.Create();
        
        var roleManagerMock = CreateRoleManagerMock();
        var service = new EscalationRuleService(db, roleManagerMock.Object);

        var request = new CreateEscalationRuleRequest
        {
            SlaPolicyId = Guid.NewGuid(),
            OverdueMinutes = 30,
            EscalationLevel = 1,
            TargetRoleName = "Admin",
            IsActive = true
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
        Assert.Contains("SlaPolicy", exception.Message);
    }
}
