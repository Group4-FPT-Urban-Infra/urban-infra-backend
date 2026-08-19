using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class DepartmentMemberServiceTests
{
    private AppDbContext _context;
    private DepartmentMemberService _departmentMemberService;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private ILogger<DepartmentMemberService> _logger;

    private const int DepartmentId = 1;
    private const int InactiveDepartmentId = 2;
    private const int NonExistentDepartmentId = 99;
    private const string StaffUserId1 = "staff-1";
    private const string StaffUserId2 = "staff-2";
    private const string ManagerUserId = "manager-1";
    private const string NonStaffUserId = "citizen-1";
    private const string InactiveUserId = "inactive-user-1";
    private const string NonExistentUserId = "non-existent-user";

    public DepartmentMemberServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        _userManagerMock = TestHelpers.CreateUserManagerMock();
        _logger = TestLogger.Create<DepartmentMemberService>();

        _departmentMemberService = new DepartmentMemberService(_context, _userManagerMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        // Roles
        _context.Roles.Add(new ApplicationRole { Id = Roles.DepartmentStaff, Name = Roles.DepartmentStaff, NormalizedName = Roles.DepartmentStaff.ToUpper() });
        _context.Roles.Add(new ApplicationRole { Id = Roles.Citizen, Name = Roles.Citizen, NormalizedName = Roles.Citizen.ToUpper() });
        _context.SaveChanges();

        // Users
        var staffUser1 = new ApplicationUser { Id = StaffUserId1, UserName = "staff1", Email = "staff1@example.com", FullName = "Staff One", IsActive = true };
        var staffUser2 = new ApplicationUser { Id = StaffUserId2, UserName = "staff2", Email = "staff2@example.com", FullName = "Staff Two", IsActive = true };
        var managerUser = new ApplicationUser { Id = ManagerUserId, UserName = "manager1", Email = "manager1@example.com", FullName = "Manager One", IsActive = true };
        var nonStaffUser = new ApplicationUser { Id = NonStaffUserId, UserName = "citizen1", Email = "citizen1@example.com", FullName = "Citizen One", IsActive = true };
        var inactiveUser = new ApplicationUser { Id = InactiveUserId, UserName = "inactive1", Email = "inactive1@example.com", FullName = "Inactive User", IsActive = false };

        _context.Users.AddRange(staffUser1, staffUser2, managerUser, nonStaffUser, inactiveUser);
        _context.SaveChanges();

        // Mock UserManager behavior
        _userManagerMock.Setup(x => x.FindByIdAsync(StaffUserId1)).ReturnsAsync(staffUser1);
        _userManagerMock.Setup(x => x.FindByIdAsync(StaffUserId2)).ReturnsAsync(staffUser2);
        _userManagerMock.Setup(x => x.FindByIdAsync(ManagerUserId)).ReturnsAsync(managerUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(NonStaffUserId)).ReturnsAsync(nonStaffUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(InactiveUserId)).ReturnsAsync(inactiveUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(NonExistentUserId)).ReturnsAsync((ApplicationUser)null!);

        _userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == StaffUserId1), Roles.DepartmentStaff)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == StaffUserId2), Roles.DepartmentStaff)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == ManagerUserId), Roles.DepartmentStaff)).ReturnsAsync(true); // Managers can also be staff
        _userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == NonStaffUserId), Roles.DepartmentStaff)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Id == InactiveUserId), Roles.DepartmentStaff)).ReturnsAsync(true);

        // Departments
        var activeDept = new Department { DepartmentId = DepartmentId, DepartmentCode = "D1", DepartmentName = "Department One", IsActive = true };
        var inactiveDept = new Department { DepartmentId = InactiveDepartmentId, DepartmentCode = "D2", DepartmentName = "Department Two", IsActive = false };
        _context.Departments.AddRange(activeDept, inactiveDept);
        _context.SaveChanges();

        // Existing Department Members
        _context.DepartmentMembers.Add(new DepartmentMember { DepartmentId = DepartmentId, UserId = StaffUserId1, IsActive = true, IsManager = false, JoinedAt = DateTime.UtcNow.AddDays(-30), JobTitle = "Technician" });
        _context.DepartmentMembers.Add(new DepartmentMember { DepartmentId = DepartmentId, UserId = StaffUserId2, IsActive = false, IsManager = false, JoinedAt = DateTime.UtcNow.AddDays(-60), LeftAt = DateTime.UtcNow.AddDays(-10), JobTitle = "Engineer" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task AssignMemberAsync_NewStaffMember_AddsSuccessfully()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = ManagerUserId, IsManager = false, JobTitle = "New Staff" };
        var initialCount = await _context.DepartmentMembers.CountAsync();

        // Act
        var result = await _departmentMemberService.AssignMemberAsync(DepartmentId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(ManagerUserId);
        result.DepartmentId.Should().Be(DepartmentId);
        result.IsManager.Should().BeFalse();
        result.IsActive.Should().BeTrue();
        result.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.JobTitle.Should().Be("New Staff");

        (await _context.DepartmentMembers.CountAsync()).Should().Be(initialCount + 1);
        var newMember = await _context.DepartmentMembers.FindAsync(DepartmentId, ManagerUserId);
        newMember.Should().NotBeNull();
        newMember!.IsActive.Should().BeTrue();
        newMember.IsManager.Should().BeFalse();
    }

    [Fact]
    public async Task AssignMemberAsync_NewManagerMember_AddsSuccessfully()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = ManagerUserId, IsManager = true, JobTitle = "Department Head" };
        var initialCount = await _context.DepartmentMembers.CountAsync();

        // Act
        var result = await _departmentMemberService.AssignMemberAsync(DepartmentId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(ManagerUserId);
        result.DepartmentId.Should().Be(DepartmentId);
        result.IsManager.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.JobTitle.Should().Be("Department Head");

        (await _context.DepartmentMembers.CountAsync()).Should().Be(initialCount + 1);
        var newMember = await _context.DepartmentMembers.FindAsync(DepartmentId, ManagerUserId);
        newMember.Should().NotBeNull();
        newMember!.IsActive.Should().BeTrue();
        newMember.IsManager.Should().BeTrue();
    }

    [Fact]
    public async Task AssignMemberAsync_DepartmentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = StaffUserId1, IsManager = false };

        // Act
        Func<Task> act = async () => await _departmentMemberService.AssignMemberAsync(NonExistentDepartmentId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Không tìm thấy đơn vị có ID = {NonExistentDepartmentId}.");
    }

    [Fact]
    public async Task AssignMemberAsync_UserNotFound_ThrowsArgumentException()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = NonExistentUserId, IsManager = false };

        // Act
        Func<Task> act = async () => await _departmentMemberService.AssignMemberAsync(DepartmentId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Người dùng có ID = {NonExistentUserId} không tồn tại.");
    }

    [Fact]
    public async Task AssignMemberAsync_UserNotDepartmentStaffRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = NonStaffUserId, IsManager = false };

        // Act
        Func<Task> act = async () => await _departmentMemberService.AssignMemberAsync(DepartmentId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Người dùng phải có vai trò DepartmentStaff trước khi được gán vào đơn vị.");
    }

    [Fact]
    public async Task AssignMemberAsync_UserAlreadyActiveMember_UpdatesExistingMember()
    {
        // Arrange
        var initialMember = await _context.DepartmentMembers.FindAsync(DepartmentId, StaffUserId1);
        initialMember.Should().NotBeNull();
        initialMember!.IsManager.Should().BeFalse();
        initialMember.JobTitle.Should().Be("Technician");

        var request = new AssignDepartmentMemberRequest { UserId = StaffUserId1, IsManager = true, JobTitle = "Senior Technician" };
        var initialCount = await _context.DepartmentMembers.CountAsync();

        // Act
        var result = await _departmentMemberService.AssignMemberAsync(DepartmentId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(StaffUserId1);
        result.IsManager.Should().BeTrue();
        result.JobTitle.Should().Be("Senior Technician");

        (await _context.DepartmentMembers.CountAsync()).Should().Be(initialCount); // No new member added
        var updatedMember = await _context.DepartmentMembers.FindAsync(DepartmentId, StaffUserId1);
        updatedMember.Should().NotBeNull();
        updatedMember!.IsManager.Should().BeTrue();
        updatedMember.JobTitle.Should().Be("Senior Technician");
        updatedMember.IsActive.Should().BeTrue();
        updatedMember.LeftAt.Should().BeNull();
    }

    [Fact]
    public async Task AssignMemberAsync_InactiveUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = InactiveUserId, IsManager = false };

        // Act
        Func<Task> act = async () => await _departmentMemberService.AssignMemberAsync(DepartmentId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Không thể gán người dùng đã bị khóa.");
    }

    [Fact]
    public async Task AssignMemberAsync_InactiveDepartment_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new AssignDepartmentMemberRequest { UserId = StaffUserId1, IsManager = false };

        // Act
        Func<Task> act = async () => await _departmentMemberService.AssignMemberAsync(InactiveDepartmentId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Không thể gán cán bộ vào đơn vị đã ngừng hoạt động.");
    }

    [Fact]
    public async Task RemoveMemberAsync_ExistingActiveMember_RemovesSuccessfully()
    {
        // Arrange
        var member = await _context.DepartmentMembers.FindAsync(DepartmentId, StaffUserId1);
        member.Should().NotBeNull();
        member!.IsActive.Should().BeTrue();

        // Act
        var result = await _departmentMemberService.RemoveMemberAsync(DepartmentId, StaffUserId1, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var removedMember = await _context.DepartmentMembers.FindAsync(DepartmentId, StaffUserId1);
        removedMember.Should().NotBeNull();
        removedMember!.IsActive.Should().BeFalse();
        removedMember.IsManager.Should().BeFalse(); // Should also set IsManager to false
        removedMember.LeftAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RemoveMemberAsync_DepartmentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        // Act
        Func<Task> act = async () => await _departmentMemberService.RemoveMemberAsync(NonExistentDepartmentId, StaffUserId1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Không tìm thấy đơn vị có ID = {NonExistentDepartmentId}.");
    }

    [Fact]
    public async Task RemoveMemberAsync_MemberNotFound_ReturnsFalse()
    {
        // Arrange
        // Act
        var result = await _departmentMemberService.RemoveMemberAsync(DepartmentId, NonExistentUserId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveMemberAsync_MemberAlreadyInactive_ReturnsFalse()
    {
        // Arrange
        var member = await _context.DepartmentMembers.FindAsync(DepartmentId, StaffUserId2);
        member.Should().NotBeNull();
        member!.IsActive.Should().BeFalse();

        // Act
        var result = await _departmentMemberService.RemoveMemberAsync(DepartmentId, StaffUserId2, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetMembersAsync_ActiveOnly_ReturnsOnlyActiveMembers()
    {
        // Arrange
        // StaffUserId1 is active, StaffUserId2 is inactive

        // Act
        var members = await _departmentMemberService.GetMembersAsync(DepartmentId, activeOnly: true, CancellationToken.None);

        // Assert
        members.Should().NotBeNull();
        members.Should().HaveCount(1);
        members.First().UserId.Should().Be(StaffUserId1);
        members.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetMembersAsync_AllMembers_ReturnsAllMembersIncludingInactive()
    {
        // Arrange
        // StaffUserId1 is active, StaffUserId2 is inactive

        // Act
        var members = await _departmentMemberService.GetMembersAsync(DepartmentId, activeOnly: false, CancellationToken.None);

        // Assert
        members.Should().NotBeNull();
        members.Should().HaveCount(2);
        members.Should().Contain(m => m.UserId == StaffUserId1 && m.IsActive);
        members.Should().Contain(m => m.UserId == StaffUserId2 && !m.IsActive);
    }

    [Fact]
    public async Task GetMembersAsync_EmptyDepartment_ReturnsEmptyList()
    {
        // Arrange
        var emptyDeptId = 3;
        _context.Departments.Add(new Department { DepartmentId = emptyDeptId, DepartmentCode = "D3", DepartmentName = "Empty Department", IsActive = true });
        _context.SaveChanges();

        // Act
        var members = await _departmentMemberService.GetMembersAsync(emptyDeptId, activeOnly: true, CancellationToken.None);

        // Assert
        members.Should().NotBeNull();
        members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMembersAsync_DepartmentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        // Act
        Func<Task> act = async () => await _departmentMemberService.GetMembersAsync(NonExistentDepartmentId, activeOnly: true, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Không tìm thấy đơn vị có ID = {NonExistentDepartmentId}.");
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsCorrectDataMapping()
    {
        // Arrange
        // StaffUserId1 is active, IsManager=false, JobTitle="Technician"

        // Act
        var members = await _departmentMemberService.GetMembersAsync(DepartmentId, activeOnly: true, CancellationToken.None);

        // Assert
        members.Should().NotBeNull();
        members.Should().HaveCount(1);
        var member = members.First();

        member.UserId.Should().Be(StaffUserId1);
        member.FullName.Should().Be("Staff One");
        member.Email.Should().Be("staff1@example.com");
        member.JobTitle.Should().Be("Technician");
        member.IsManager.Should().BeFalse();
        member.IsActive.Should().BeTrue();
        member.DepartmentId.Should().Be(DepartmentId);
        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-30), TimeSpan.FromSeconds(5));
        member.LeftAt.Should().BeNull();
    }
}