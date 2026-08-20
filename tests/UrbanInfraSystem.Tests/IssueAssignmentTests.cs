using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.IssueAssignments;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueAssignmentTests
{
    [Fact]
    public async Task ReassignAsync_Admin_ShouldSuccessfullyReassign_AndResetStatusToNew()
    {
        // Arrange
        using var db = TestDb.Create();

        // Seed Statuses
        db.IssueStatuses.Add(new IssueStatus { StatusId = 1, StatusCode = "NEW", StatusName = "Mới tiếp nhận", DisplayOrder = 1, IsActive = true });
        db.IssueStatuses.Add(new IssueStatus { StatusId = 2, StatusCode = "IN_PROGRESS", StatusName = "Đang xử lý", DisplayOrder = 2, IsActive = true });

        // Seed Departments
        var deptOld = new Department { DepartmentId = 1, DepartmentCode = "DEPT_1", DepartmentName = "Đơn vị Cũ", IsActive = true };
        var deptNew = new Department { DepartmentId = 2, DepartmentCode = "DEPT_2", DepartmentName = "Đơn vị Mới", IsActive = true };
        db.Departments.AddRange(deptOld, deptNew);

        // Seed Issue with current assignment
        var issue = new Issue
        {
            IssueId = 101,
            PublicCode = "ISS-101",
            ReporterId = "user-reporter-1",
            StatusId = 2,
            Title = "Báo cáo ổ gà",
            Description = "Mô tả ổ gà"
        };
        db.Issues.Add(issue);

        var currentAssignment = new IssueAssignment
        {
            IssueId = 101,
            DepartmentId = 1,
            AssignmentMethod = "MANUAL",
            AssignedAt = DateTime.UtcNow.AddDays(-1),
            IsCurrent = true
        };
        db.IssueAssignments.Add(currentAssignment);

        // Add an assignment member to test member removal on reassign
        db.IssueAssignmentMembers.Add(new IssueAssignmentMember
        {
            AssignmentId = currentAssignment.AssignmentId,
            UserId = "staff-user-1",
            AssignedBy = "admin-user",
            AssignedAt = DateTime.UtcNow.AddDays(-1)
        });

        await db.SaveChangesAsync();

        var service = new IssueAssignmentService(db);
        var request = new ReassignIssueRequest { DepartmentId = 2, Note = "Chuyển đơn vị quản lý chuyên môn" };

        // Act
        var result = await service.ReassignAsync(
            issueId: 101,
            request: request,
            actorUserId: "admin-user",
            isAdmin: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.IssueId);
        Assert.Equal(2, result.DepartmentId);
        Assert.Equal("Đơn vị Mới", result.DepartmentName);
        Assert.True(result.IsCurrent);

        // Verify previous assignment was closed
        var oldAssigned = await db.IssueAssignments.FirstAsync(a => a.DepartmentId == 1);
        Assert.False(oldAssigned.IsCurrent);
        Assert.NotNull(oldAssigned.EndedAt);

        // Verify issue status reset to 1 ("NEW")
        var updatedIssue = await db.Issues.FirstAsync(i => i.IssueId == 101);
        Assert.Equal(1, updatedIssue.StatusId);

        // Verify IssueUpdate logs created
        var updates = await db.IssueUpdates.Where(u => u.IssueId == 101).ToListAsync();
        Assert.NotEmpty(updates);

        // Verify Notification created for department members & reporter
        var notifications = await db.Notifications.Where(n => n.IssueId == 101).ToListAsync();
        Assert.NotEmpty(notifications);
    }

    [Fact]
    public async Task ReassignAsync_ShouldThrowKeyNotFoundException_WhenIssueDoesNotExist()
    {
        // Arrange
        using var db = TestDb.Create();
        var service = new IssueAssignmentService(db);
        var request = new ReassignIssueRequest { DepartmentId = 1, Note = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ReassignAsync(9999, request, "admin-user", true));
    }

    [Fact]
    public async Task ReassignAsync_ShouldThrowArgumentException_WhenDepartmentDoesNotExistOrInactive()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Issues.Add(new Issue { IssueId = 201, PublicCode = "ISS-201", ReporterId = "user-1", Title = "Test", Description = "Test" });
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "DEPT_INACTIVE", DepartmentName = "Đơn vị khóa", IsActive = false });
        await db.SaveChangesAsync();

        var service = new IssueAssignmentService(db);
        var request = new ReassignIssueRequest { DepartmentId = 1, Note = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ReassignAsync(201, request, "admin-user", true));
    }

    [Fact]
    public async Task ReassignAsync_ShouldThrowInvalidOperationException_WhenReassigningToSameCurrentDepartment()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Issues.Add(new Issue { IssueId = 301, PublicCode = "ISS-301", ReporterId = "user-1", Title = "Test", Description = "Test" });
        db.Departments.Add(new Department { DepartmentId = 5, DepartmentCode = "DEPT_5", DepartmentName = "Đơn vị 5", IsActive = true });
        db.IssueAssignments.Add(new IssueAssignment { IssueId = 301, DepartmentId = 5, AssignmentMethod = "MANUAL", IsCurrent = true });
        await db.SaveChangesAsync();

        var service = new IssueAssignmentService(db);
        var request = new ReassignIssueRequest { DepartmentId = 5, Note = "Trùng đơn vị" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReassignAsync(301, request, "admin-user", true));
    }

    [Fact]
    public async Task ReassignAsync_ShouldThrowUnauthorizedAccessException_WhenNonAdminUserIsNotDepartmentManager()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Issues.Add(new Issue { IssueId = 401, PublicCode = "ISS-401", ReporterId = "user-1", Title = "Test", Description = "Test" });
        db.Departments.Add(new Department { DepartmentId = 1, DepartmentCode = "DEPT_1", DepartmentName = "Đơn vị 1", IsActive = true });
        db.Departments.Add(new Department { DepartmentId = 2, DepartmentCode = "DEPT_2", DepartmentName = "Đơn vị 2", IsActive = true });
        db.IssueAssignments.Add(new IssueAssignment { IssueId = 401, DepartmentId = 1, AssignmentMethod = "MANUAL", IsCurrent = true });

        // Add user as regular staff (not manager)
        db.DepartmentMembers.Add(new DepartmentMember { DepartmentId = 1, UserId = "staff-user", IsManager = false, IsActive = true });
        await db.SaveChangesAsync();

        var service = new IssueAssignmentService(db);
        var request = new ReassignIssueRequest { DepartmentId = 2, Note = "Test non manager" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ReassignAsync(401, request, "staff-user", isAdmin: false));
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAssignmentHistory_OrderedDescending()
    {
        // Arrange
        using var db = TestDb.Create();
        db.Issues.Add(new Issue { IssueId = 501, PublicCode = "ISS-501", ReporterId = "user-1", Title = "Test", Description = "Test" });
        var dept1 = new Department { DepartmentId = 1, DepartmentCode = "D1", DepartmentName = "Đơn vị 1", IsActive = true };
        var dept2 = new Department { DepartmentId = 2, DepartmentCode = "D2", DepartmentName = "Đơn vị 2", IsActive = true };
        db.Departments.AddRange(dept1, dept2);

        db.IssueAssignments.Add(new IssueAssignment
        {
            IssueId = 501,
            DepartmentId = 1,
            AssignmentMethod = "MANUAL",
            AssignedAt = DateTime.UtcNow.AddDays(-5),
            EndedAt = DateTime.UtcNow.AddDays(-2),
            IsCurrent = false
        });
        db.IssueAssignments.Add(new IssueAssignment
        {
            IssueId = 501,
            DepartmentId = 2,
            AssignmentMethod = "MANUAL",
            AssignedAt = DateTime.UtcNow.AddDays(-2),
            IsCurrent = true
        });

        await db.SaveChangesAsync();

        var service = new IssueAssignmentService(db);

        // Act
        var history = await service.GetHistoryAsync(501);

        // Assert
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.Equal(2, history[0].DepartmentId); // Most recent first
        Assert.Equal(1, history[1].DepartmentId);

        // Non existent issue returns null
        var nonExistentHistory = await service.GetHistoryAsync(9999);
        Assert.Null(nonExistentHistory);
    }
}
