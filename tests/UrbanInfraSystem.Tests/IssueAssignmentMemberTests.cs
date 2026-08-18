using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueAssignmentMemberTests
{
    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mgr;
    }

    [Fact]
    public async Task AssignMemberAsync_ShouldSuccessfullyAssignStaffToDepartment()
    {
        // Arrange
        using var db = TestDb.Create();
        var dept = new Department { DepartmentId = 1, DepartmentCode = "DEPT_1", DepartmentName = "Phòng Giao Thông", IsActive = true };
        db.Departments.Add(dept);

        var staffUser = new ApplicationUser
        {
            Id = "staff-1",
            UserName = "staff1@urban.local",
            Email = "staff1@urban.local",
            FullName = "Cán bộ 1",
            IsActive = true
        };
        db.Users.Add(staffUser);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync("staff-1")).ReturnsAsync(staffUser);
        mockUserManager.Setup(m => m.IsInRoleAsync(staffUser, Roles.DepartmentStaff)).ReturnsAsync(true);

        var service = new DepartmentMemberService(db, mockUserManager.Object);
        var request = new AssignDepartmentMemberRequest
        {
            UserId = "staff-1",
            JobTitle = "Chuyên viên kỹ thuật",
            IsManager = false
        };

        // Act
        var result = await service.AssignMemberAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DepartmentId);
        Assert.Equal("staff-1", result.UserId);
        Assert.Equal("Cán bộ 1", result.FullName);
        Assert.Equal("Chuyên viên kỹ thuật", result.JobTitle);
        Assert.False(result.IsManager);
        Assert.True(result.IsActive);

        // Verify DB record
        var memberInDb = await db.DepartmentMembers.FindAsync(1, "staff-1");
        Assert.NotNull(memberInDb);
        Assert.True(memberInDb.IsActive);
    }

    [Fact]
    public async Task AssignMemberAsync_ShouldThrowArgumentException_WhenUserNotFound()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Phòng 1", IsActive = true });
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync("non-existent")).ReturnsAsync((ApplicationUser?)null);

        var service = new DepartmentMemberService(db, mockUserManager.Object);
        var request = new AssignDepartmentMemberRequest { UserId = "non-existent" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.AssignMemberAsync(1, request));
    }

    [Fact]
    public async Task AssignMemberAsync_ShouldThrowInvalidOperationException_WhenUserIsInactiveOrNotDepartmentStaff()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Phòng 1", IsActive = true });

        var inactiveUser = new ApplicationUser { Id = "inactive-1", FullName = "User Blocked", IsActive = false };
        var citizenUser = new ApplicationUser { Id = "citizen-1", FullName = "Công Dân 1", IsActive = true };
        db.Users.AddRange(inactiveUser, citizenUser);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync("inactive-1")).ReturnsAsync(inactiveUser);
        mockUserManager.Setup(m => m.FindByIdAsync("citizen-1")).ReturnsAsync(citizenUser);
        mockUserManager.Setup(m => m.IsInRoleAsync(citizenUser, Roles.DepartmentStaff)).ReturnsAsync(false);

        var service = new DepartmentMemberService(db, mockUserManager.Object);

        // 1. Test Inactive User
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignMemberAsync(1, new AssignDepartmentMemberRequest { UserId = "inactive-1" }));

        // 2. Test Non-DepartmentStaff User
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignMemberAsync(1, new AssignDepartmentMemberRequest { UserId = "citizen-1" }));
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldDeactivateDepartmentMember()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Phòng 1", IsActive = true });
        db.DepartmentMembers.Add(new DepartmentMember
        {
            DepartmentId = 1,
            UserId = "staff-1",
            IsManager = true,
            IsActive = true,
            JoinedAt = DateTime.UtcNow.AddMonths(-2)
        });
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        var service = new DepartmentMemberService(db, mockUserManager.Object);

        // Act
        var success = await service.RemoveMemberAsync(1, "staff-1");

        // Assert
        Assert.True(success);

        var memberInDb = await db.DepartmentMembers.FindAsync(1, "staff-1");
        Assert.NotNull(memberInDb);
        Assert.False(memberInDb.IsActive);
        Assert.False(memberInDb.IsManager);
        Assert.NotNull(memberInDb.LeftAt);

        // Removing non-existent member returns false
        var removeNotExist = await service.RemoveMemberAsync(1, "non-existent");
        Assert.False(removeNotExist);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldReturnDepartmentMembers_FilteredByActiveOnly()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Phòng 1", IsActive = true });

        var user1 = new ApplicationUser { Id = "u1", FullName = "An Nguyen", Email = "an@urban.local" };
        var user2 = new ApplicationUser { Id = "u2", FullName = "Binh Tran", Email = "binh@urban.local" };
        db.Users.AddRange(user1, user2);

        db.DepartmentMembers.Add(new DepartmentMember { DepartmentId = 1, UserId = "u1", IsActive = true, JobTitle = "Staff" });
        db.DepartmentMembers.Add(new DepartmentMember { DepartmentId = 1, UserId = "u2", IsActive = false, JobTitle = "Former Staff", LeftAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        var service = new DepartmentMemberService(db, mockUserManager.Object);

        // Act: Get active only (default true)
        var activeMembers = await service.GetMembersAsync(1, activeOnly: true);

        // Assert
        Assert.Single(activeMembers);
        Assert.Equal("u1", activeMembers[0].UserId);
        Assert.Equal("An Nguyen", activeMembers[0].FullName);

        // Act: Get all including inactive
        var allMembers = await service.GetMembersAsync(1, activeOnly: false);
        Assert.Equal(2, allMembers.Count);
    }
}
