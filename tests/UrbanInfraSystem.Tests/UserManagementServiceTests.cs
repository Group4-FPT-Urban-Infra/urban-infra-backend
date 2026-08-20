using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Application.DTOs.UserManagement;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class UserManagementServiceTests
{
    private AppDbContext _context;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private Mock<IDepartmentMemberService> _departmentMemberServiceMock;
    private ICurrentUserService _currentUser;
    private UserManagementService _service;

    private List<ApplicationUser> _users;
    private List<ApplicationRole> _roles;

    public UserManagementServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        SeedData();

        _userManagerMock = TestHelpers.CreateUserManagerMock(_users);
        _roleManagerMock = TestHelpers.CreateRoleManagerMock(_roles);
        _departmentMemberServiceMock = new Mock<IDepartmentMemberService>();
        _currentUser = new TestCurrentUser("admin-id", roles: new[] { Roles.Admin });

        _service = new UserManagementService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _context,
            _departmentMemberServiceMock.Object,
            _currentUser
        );
    }

    private void SeedData()
    {
        _roles = new List<ApplicationRole>
        {
            new() { Name = Roles.Admin, NormalizedName = Roles.Admin.ToUpper() },
            new() { Name = Roles.DepartmentStaff, NormalizedName = Roles.DepartmentStaff.ToUpper() },
            new() { Name = Roles.Citizen, NormalizedName = Roles.Citizen.ToUpper() }
        };

        _users = new List<ApplicationUser>
        {
            new() { Id = "admin-1", FullName = "Admin User", Email = "admin@test.com", UserName = "admin@test.com", IsActive = true },
            new() { Id = "staff-1", FullName = "Staff User 1", Email = "staff1@test.com", UserName = "staff1@test.com", IsActive = true },
            new() { Id = "staff-2", FullName = "Staff User 2", Email = "staff2@test.com", UserName = "staff2@test.com", IsActive = false },
            new() { Id = "citizen-1", FullName = "Citizen User", Email = "citizen@test.com", UserName = "citizen@test.com", IsActive = true }
        };

        _context.Departments.AddRange(
            new Department { DepartmentId = 1, DepartmentName = "Dept A" },
            new Department { DepartmentId = 2, DepartmentName = "Dept B" }
        );

        _context.DepartmentMembers.Add(new DepartmentMember { DepartmentId = 1, UserId = "staff-1", IsActive = true });

        _context.SaveChanges();
    }

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidDataForStaff_CreatesUserAndAssignsToDepartment()
    {
        // Arrange
        var request = new CreateUserByAdminRequest
        {
            Email = "new.staff@test.com",
            FullName = "New Staff",
            Password = "Password123!",
            Role = Roles.DepartmentStaff,
            DepartmentId = 1
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.Roles.Should().Contain(Roles.DepartmentStaff);
        result.DepartmentId.Should().Be(1);

        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == request.Email), request.Password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.DepartmentStaff), Times.Once);
        _departmentMemberServiceMock.Verify(x => x.AssignMemberAsync(1, It.Is<AssignDepartmentMemberRequest>(r => !string.IsNullOrEmpty(r.UserId)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserByAdminRequest { Email = "admin@test.com", FullName = "Fail", Password = "p", Role = Roles.Admin };

        // Act
        Func<Task> act = () => _service.CreateUserAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*đã được sử dụng*");
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidRole_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserByAdminRequest { Email = "new@test.com", FullName = "Fail", Password = "p", Role = "InvalidRole" };

        // Act
        Func<Task> act = () => _service.CreateUserAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*không hợp lệ*");
    }

    [Fact]
    public async Task CreateUserAsync_ForStaffWithoutDepartmentId_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateUserByAdminRequest { Email = "new@test.com", FullName = "Fail", Password = "p", Role = Roles.DepartmentStaff, DepartmentId = null };

        // Act
        Func<Task> act = () => _service.CreateUserAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*DepartmentId là bắt buộc*");
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidData_UpdatesInfoAndRole()
    {
        // Arrange
        var userId = "staff-1";
        var userToUpdate = _users.First(u => u.Id == userId);
        _userManagerMock.Setup(x => x.GetRolesAsync(userToUpdate)).ReturnsAsync(new List<string> { Roles.DepartmentStaff });

        var request = new UpdateUserByAdminRequest
        {
            FullName = "Updated Staff Name",
            PhoneNumber = "0987654321",
            Role = Roles.Admin,
            DepartmentId = null // Admin has no department
        };

        // Act
        var result = await _service.UpdateUserAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("Updated Staff Name");
        result.PhoneNumber.Should().Be("0987654321");
        result.Roles.Should().Contain(Roles.Admin);

        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.FullName == request.FullName)), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(userToUpdate, It.Is<IEnumerable<string>>(roles => roles.Contains(Roles.DepartmentStaff))), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(userToUpdate, Roles.Admin), Times.Once);
        _departmentMemberServiceMock.Verify(x => x.RemoveMemberAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateUserByAdminRequest { FullName = "Fail", Role = Roles.Admin };

        // Act
        Func<Task> act = () => _service.UpdateUserAsync("non-existent-id", request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateUserAsync_ChangingDepartment_CallsDepartmentMemberService()
    {
        // Arrange
        var userId = "staff-1";
        var userToUpdate = _users.First(u => u.Id == userId);
        _userManagerMock.Setup(x => x.GetRolesAsync(userToUpdate)).ReturnsAsync(new List<string> { Roles.DepartmentStaff });

        var request = new UpdateUserByAdminRequest
        {
            FullName = userToUpdate.FullName,
            Role = Roles.DepartmentStaff,
            DepartmentId = 2 // Moving from Dept 1 to Dept 2
        };

        // Act
        await _service.UpdateUserAsync(userId, request);

        // Assert
        _departmentMemberServiceMock.Verify(x => x.RemoveMemberAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
        _departmentMemberServiceMock.Verify(x => x.AssignMemberAsync(2, It.Is<AssignDepartmentMemberRequest>(r => r.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SetUserActiveStatusAsync Tests

    [Fact]
    public async Task SetUserActiveStatusAsync_ToInactive_SetsIsActiveToFalse()
    {
        // Arrange
        var userId = "staff-1";
        var user = _users.First(u => u.Id == userId);
        user.IsActive = true;

        // Act
        var result = await _service.SetUserActiveStatusAsync(userId, false);

        // Assert
        result.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.Id == userId && !u.IsActive)), Times.Once);
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        // Act
        var result = await _service.SetUserActiveStatusAsync("non-existent-id", false);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidUser_ResetsPasswordSuccessfully()
    {
        // Arrange
        var userId = "staff-1";
        var user = _users.First(u => u.Id == userId);
        var request = new AdminResetPasswordRequest { NewPassword = "NewPassword123!" };

        _userManagerMock.Setup(x => x.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddPasswordAsync(user, request.NewPassword)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ResetPasswordAsync(userId, request);

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(x => x.RemovePasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.AddPasswordAsync(user, request.NewPassword), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var request = new AdminResetPasswordRequest { NewPassword = "p" };

        // Act
        var result = await _service.ResetPasswordAsync("non-existent-id", request);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_WithNoFilters_ReturnsAllUsersPaged()
    {
        // Arrange
        var request = new AdminUserListRequest { Page = 1, PageSize = 2 };
        // Mock GetRolesAsync for each user
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "SomeRole" });

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(4);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetUsersAsync_WithKeywordFilter_ReturnsMatchingUsers()
    {
        // Arrange
        var request = new AdminUserListRequest { Keyword = "Staff User 1" };
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.Items.Should().ContainSingle().Which.Id.Should().Be("staff-1");
    }

    [Fact]
    public async Task GetUsersAsync_WithRoleFilter_ReturnsUsersInRole()
    {
        // Arrange
        var request = new AdminUserListRequest { Role = Roles.Citizen };
        var citizenUser = _users.First(u => u.Id == "citizen-1");
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(Roles.Citizen)).ReturnsAsync(new List<ApplicationUser> { citizenUser });
        _userManagerMock.Setup(x => x.GetRolesAsync(citizenUser)).ReturnsAsync(new List<string> { Roles.Citizen });

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.Items.Should().ContainSingle().Which.Id.Should().Be("citizen-1");
    }

    [Fact]
    public async Task GetUsersAsync_WithIsActiveFilter_ReturnsMatchingUsers()
    {
        // Arrange
        var request = new AdminUserListRequest { IsActive = false };
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.Items.Should().ContainSingle().Which.Id.Should().Be("staff-2");
    }

    [Fact]
    public async Task GetUsersAsync_WithDepartmentFilter_ReturnsUsersInDepartment()
    {
        // Arrange
        var request = new AdminUserListRequest { DepartmentId = 1 };
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string>());

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        result.Items.Should().ContainSingle().Which.Id.Should().Be("staff-1");
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithExistingUser_DeletesSuccessfully()
    {
        // Arrange
        var userId = "citizen-1";
        var user = _users.First(u => u.Id == userId);
        _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.DeleteUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        // Act
        var result = await _service.DeleteUserAsync("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}