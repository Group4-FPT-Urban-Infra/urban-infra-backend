using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class DepartmentManagerStaffServiceTests
{
    private AppDbContext _context;
    private DepartmentManagerStaffService _service;

    // Constants for IDs
    private const int Dept1Id = 1;
    private const int Dept2Id = 2;
    private const string Staff1Id = "staff-1"; // Active, has tasks
    private const string Staff2Id = "staff-2"; // Active, no tasks
    private const string Staff3Id = "staff-3"; // Inactive
    private const string StaffOtherDeptId = "staff-other-dept";

    public DepartmentManagerStaffServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        _service = new DepartmentManagerStaffService(_context);
        SeedDataAsync().GetAwaiter().GetResult();
    }

    private async Task SeedDataAsync()
    {
        // Departments
        var dept1 = new Department { DepartmentId = Dept1Id, DepartmentName = "Phòng Vận hành" };
        var dept2 = new Department { DepartmentId = Dept2Id, DepartmentName = "Phòng Kỹ thuật" };
        _context.Departments.AddRange(dept1, dept2);

        // Users
        var staff1 = new ApplicationUser { Id = Staff1Id, FullName = "Nhân viên A", Email = "a@test.com", IsActive = true, CreatedAtUtc = DateTime.UtcNow.AddMonths(-6) };
        var staff2 = new ApplicationUser { Id = Staff2Id, FullName = "Nhân viên B", Email = "b@test.com", IsActive = true, CreatedAtUtc = DateTime.UtcNow.AddMonths(-3) };
        var staff3 = new ApplicationUser { Id = Staff3Id, FullName = "Nhân viên C (Nghỉ)", Email = "c@test.com", IsActive = false, CreatedAtUtc = DateTime.UtcNow.AddMonths(-12) };
        var staffOther = new ApplicationUser { Id = StaffOtherDeptId, FullName = "Nhân viên D", Email = "d@test.com", IsActive = true };
        _context.Users.AddRange(staff1, staff2, staff3, staffOther);

        // Department Members
        _context.DepartmentMembers.AddRange(
            new DepartmentMember { DepartmentId = Dept1Id, UserId = Staff1Id, IsActive = true },
            new DepartmentMember { DepartmentId = Dept1Id, UserId = Staff2Id, IsActive = true },
            new DepartmentMember { DepartmentId = Dept1Id, UserId = Staff3Id, IsActive = false }, // Inactive member
            new DepartmentMember { DepartmentId = Dept2Id, UserId = StaffOtherDeptId, IsActive = true }
        );

        // Issues
        var issue1 = new Issue { IssueId = 101, Title = "Sự cố 101", PublicCode = "SC-101" };
        var issue2 = new Issue { IssueId = 102, Title = "Sự cố 102", PublicCode = "SC-102" };
        var issue3 = new Issue { IssueId = 103, Title = "Sự cố 103", PublicCode = "SC-103" };
        var issueOtherDept = new Issue { IssueId = 201, Title = "Sự cố phòng ban khác", PublicCode = "SC-201" };
        _context.Issues.AddRange(issue1, issue2, issue3, issueOtherDept);

        // Issue Assignments (to departments)
        var assignment1 = new IssueAssignment { AssignmentId = 1, IssueId = 101, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment2 = new IssueAssignment { AssignmentId = 2, IssueId = 102, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment3 = new IssueAssignment { AssignmentId = 3, IssueId = 103, DepartmentId = Dept1Id, IsCurrent = true };
        var assignmentOther = new IssueAssignment { AssignmentId = 4, IssueId = 201, DepartmentId = Dept2Id, IsCurrent = true };
        _context.IssueAssignments.AddRange(assignment1, assignment2, assignment3, assignmentOther);

        // Issue Assignment Members (to specific staff)
        _context.IssueAssignmentMembers.AddRange(
            // Staff 1 has 3 tasks: 1 pending, 1 accepted, 1 completed
            new IssueAssignmentMember { AssignmentId = 1, UserId = Staff1Id, Status = AssignmentMemberStatus.Pending, AssignedAt = DateTime.UtcNow.AddDays(-1) },
            new IssueAssignmentMember { AssignmentId = 2, UserId = Staff1Id, Status = AssignmentMemberStatus.Accepted, AssignedAt = DateTime.UtcNow.AddDays(-2) },
            new IssueAssignmentMember { AssignmentId = 3, UserId = Staff1Id, Status = AssignmentMemberStatus.Completed, AssignedAt = DateTime.UtcNow.AddDays(-3), EndedAt = DateTime.UtcNow.AddDays(-2) }
        );

        await _context.SaveChangesAsync();
    }

    #region GetStaffsAsync Tests

    [Fact]
    public async Task GetStaffsAsync_ForDepartmentWithWorkload_ReturnsOnlyActiveMembersWithCorrectStats()
    {
        // Arrange
        // Data seeded in Setup(): Dept 1 has 2 active members (Staff1, Staff2) and 1 inactive (Staff3).
        // Staff1 has 3 assignments: 1 pending, 1 accepted, 1 completed.
        // Staff2 has 0 assignments.

        // Act
        var result = await _service.GetStaffsAsync(Dept1Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2, "vì chỉ có 2 nhân viên active trong phòng ban");
        result.Select(s => s.UserId).Should().NotContain(Staff3Id, "vì nhân viên này inactive");

        var staff1Stats = result.FirstOrDefault(s => s.UserId == Staff1Id);
        staff1Stats.Should().NotBeNull();
        staff1Stats!.FullName.Should().Be("Nhân viên A");
        staff1Stats.AssignedCount.Should().Be(3, "vì được gán 3 sự cố");
        staff1Stats.ResolvedCount.Should().Be(1, "vì đã hoàn thành 1 sự cố");
        staff1Stats.PendingCount.Should().Be(2, "vì có 1 sự cố Pending và 1 Accepted");
        staff1Stats.CurrentWorkload.Should().Be(2);
        staff1Stats.ResolutionRate.Should().BeApproximately(33.3, 0.1, "vì (1/3)*100");

        var staff2Stats = result.FirstOrDefault(s => s.UserId == Staff2Id);
        staff2Stats.Should().NotBeNull();
        staff2Stats!.FullName.Should().Be("Nhân viên B");
        staff2Stats.AssignedCount.Should().Be(0);
        staff2Stats.ResolvedCount.Should().Be(0);
        staff2Stats.PendingCount.Should().Be(0);
        staff2Stats.CurrentWorkload.Should().Be(0);
        staff2Stats.ResolutionRate.Should().Be(0);
    }

    [Fact]
    public async Task GetStaffsAsync_ForDepartmentWithNoMembers_ReturnsEmptyList()
    {
        // Arrange
        var emptyDeptId = 99;
        _context.Departments.Add(new Department { DepartmentId = emptyDeptId, DepartmentName = "Phòng trống" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStaffsAsync(emptyDeptId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region GetStaffDetailAsync Tests

    [Fact]
    public async Task GetStaffDetailAsync_ForExistingStaffWithWorkload_ReturnsCorrectDetailsAndAssignments()
    {
        // Arrange
        // Staff1 is seeded with 3 assignments.

        // Act
        var result = await _service.GetStaffDetailAsync(Staff1Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(Staff1Id);
        result.FullName.Should().Be("Nhân viên A");
        result.AssignedCount.Should().Be(3);
        result.ResolvedCount.Should().Be(1);
        result.PendingCount.Should().Be(2);
        result.ResolutionRate.Should().BeApproximately(33.3, 0.1);

        result.RecentAssignments.Should().HaveCount(3);
        result.RecentAssignments.Select(a => a.IssueId).Should().ContainInOrder([101L, 102L, 103L]);    }

    [Fact]
    public async Task GetStaffDetailAsync_ForNonExistentStaff_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = "does-not-exist";

        // Act
        var result = await _service.GetStaffDetailAsync(nonExistentUserId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}