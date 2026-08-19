using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.DepartmentManager;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class DepartmentManagerIssueServiceTests
{
    private AppDbContext _context;
    private DepartmentManagerIssueService _service;

    // Constants for IDs
    private const int Dept1Id = 1;
    private const int Dept2Id = 2;
    private const string ManagerId = "manager-1";
    private const string Staff1Id = "staff-1";
    private const string Staff2Id = "staff-2";
    private const long UnassignedIssueId = 101;
    private const long AssignedIssueId = 102;
    private const long ResolvedIssueId = 103;
    private const long OtherDeptIssueId = 201;
    private const int StatusNewId = 1;
    private const int StatusInProgressId = 2;
    private const int StatusResolvedId = 3;
    private const int StatusClosedId = 4;
    private const int PrioHighId = 1;
    private const int PrioLowId = 2;
    private const int TypeElectricId = 1;
    private const int TypeWaterId = 2;

    public DepartmentManagerIssueServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        _service = new DepartmentManagerIssueService(_context);
        SeedDataAsync().GetAwaiter().GetResult();
    }

    private async Task SeedDataAsync()
    {
        // Lookups
        var dept1 = new Department { DepartmentId = Dept1Id, DepartmentName = "Phòng Vận hành" };
        var dept2 = new Department { DepartmentId = Dept2Id, DepartmentName = "Phòng Kỹ thuật" };
        _context.Departments.AddRange(dept1, dept2);

        var manager = new ApplicationUser { Id = ManagerId, FullName = "Trưởng phòng A" };
        var staff1 = new ApplicationUser { Id = Staff1Id, FullName = "Nhân viên A1" };
        var staff2 = new ApplicationUser { Id = Staff2Id, FullName = "Nhân viên A2" };
        _context.Users.AddRange(manager, staff1, staff2);

        _context.DepartmentMembers.AddRange(
            new DepartmentMember { DepartmentId = Dept1Id, UserId = ManagerId, IsManager = true },
            new DepartmentMember { DepartmentId = Dept1Id, UserId = Staff1Id },
            new DepartmentMember { DepartmentId = Dept1Id, UserId = Staff2Id }
        );

        var statusNew = new IssueStatus { StatusId = StatusNewId, StatusCode = "NEW", StatusName = "Mới" };
        var statusInProgress = new IssueStatus { StatusId = StatusInProgressId, StatusCode = "IN_PROGRESS", StatusName = "Đang xử lý" };
        var statusResolved = new IssueStatus { StatusId = StatusResolvedId, StatusCode = "RESOLVED", StatusName = "Đã giải quyết", IsClosed = true };
        var statusClosed = new IssueStatus { StatusId = StatusClosedId, StatusCode = "CLOSED", StatusName = "Đã đóng", IsClosed = true };
        _context.IssueStatuses.AddRange(statusNew, statusInProgress, statusResolved, statusClosed);

        var prioHigh = new IssuePriority { PriorityId = PrioHighId, PriorityName = "Cao" };
        var prioLow = new IssuePriority { PriorityId = PrioLowId, PriorityName = "Thấp" };
        _context.IssuePriorities.AddRange(prioHigh, prioLow);

        var typeElectric = new IssueType { IssueTypeId = TypeElectricId, TypeName = "Sự cố điện" };
        var typeWater = new IssueType { IssueTypeId = TypeWaterId, TypeName = "Sự cố nước" };
        _context.IssueTypes.AddRange(typeElectric, typeWater);

        // Issues
        var issue1 = new Issue { IssueId = UnassignedIssueId, Title = "Unassigned Electric Issue", StatusId = StatusNewId, PriorityId = PrioHighId, IssueTypeId = TypeElectricId, ReportedAt = DateTime.UtcNow.AddDays(-2) };
        var issue2 = new Issue { IssueId = AssignedIssueId, Title = "Assigned Water Issue", StatusId = StatusInProgressId, PriorityId = PrioLowId, IssueTypeId = TypeWaterId, ReportedAt = DateTime.UtcNow.AddDays(-1) };
        var issue3 = new Issue { IssueId = ResolvedIssueId, Title = "Resolved Electric Issue", StatusId = StatusResolvedId, PriorityId = PrioLowId, IssueTypeId = TypeElectricId, ReportedAt = DateTime.UtcNow.AddDays(-5), ResolvedAt = DateTime.UtcNow.AddDays(-1) };
        var issue4 = new Issue { IssueId = OtherDeptIssueId, Title = "Other Department Issue", StatusId = StatusNewId, PriorityId = PrioHighId, IssueTypeId = TypeElectricId, ReportedAt = DateTime.UtcNow.AddDays(-1) };
        _context.Issues.AddRange(issue1, issue2, issue3, issue4);

        // Assignments
        var assignment1 = new IssueAssignment { AssignmentId = 1, IssueId = UnassignedIssueId, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment2 = new IssueAssignment { AssignmentId = 2, IssueId = AssignedIssueId, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment3 = new IssueAssignment { AssignmentId = 3, IssueId = ResolvedIssueId, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment4 = new IssueAssignment { AssignmentId = 4, IssueId = OtherDeptIssueId, DepartmentId = Dept2Id, IsCurrent = true };
        _context.IssueAssignments.AddRange(assignment1, assignment2, assignment3, assignment4);

        // Assignment Members
        _context.IssueAssignmentMembers.Add(new IssueAssignmentMember { AssignmentId = 2, UserId = Staff1Id, Status = AssignmentMemberStatus.Accepted });

        await _context.SaveChangesAsync();
    }

    #region GetIssuesAsync Tests

    [Fact]
    public async Task GetIssuesAsync_WithNoFilter_ReturnsAllDepartmentIssues()
    {
        // Arrange
        var request = new DepartmentManagerIssueListRequest();

        // Act
        var result = await _service.GetIssuesAsync(Dept1Id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Select(i => i.IssueId).Should().Contain(new[] { UnassignedIssueId, AssignedIssueId, ResolvedIssueId });
        result.Items.Select(i => i.IssueId).Should().NotContain(OtherDeptIssueId);
    }

    [Fact]
    public async Task GetIssuesAsync_WithStatusFilter_ReturnsFilteredIssues()
    {
        // Arrange
        var request = new DepartmentManagerIssueListRequest { StatusIds = new List<int> { StatusInProgressId } };

        // Act
        var result = await _service.GetIssuesAsync(Dept1Id, request, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.IssueId.Should().Be(AssignedIssueId);
    }

    [Fact]
    public async Task GetIssuesAsync_WithPriorityAndTypeFilters_ReturnsFilteredIssues()
    {
        // Arrange
        var request = new DepartmentManagerIssueListRequest
        {
            PriorityIds = new List<int> { PrioHighId },
            IssueTypeIds = new List<int> { TypeElectricId }
        };

        // Act
        var result = await _service.GetIssuesAsync(Dept1Id, request, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.IssueId.Should().Be(UnassignedIssueId);
    }

    [Fact]
    public async Task GetIssuesAsync_WithUnassignedFilter_ReturnsOnlyUnassignedIssues()
    {
        // Arrange
        var request = new DepartmentManagerIssueListRequest { Filter = "unassigned" };

        // Act
        var result = await _service.GetIssuesAsync(Dept1Id, request, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2, "vì có 2 sự cố (101, 103) chưa được gán cho nhân viên nào");
        result.Items.Select(i => i.IssueId).Should().BeEquivalentTo(new[] { UnassignedIssueId, ResolvedIssueId });
    }

    [Fact]
    public async Task GetIssuesAsync_WithTeamAssignedFilter_ReturnsOnlyAssignedToMemberIssues()
    {
        // Arrange
        var request = new DepartmentManagerIssueListRequest { Filter = "team_assigned" };

        // Act
        var result = await _service.GetIssuesAsync(Dept1Id, request, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.IssueId.Should().Be(AssignedIssueId);
    }

    #endregion

    #region AssignIssueAsync Tests

    [Fact]
    public async Task AssignIssueAsync_ValidRequest_CreatesMemberAssignmentAndLogsUpdate()
    {
        // Arrange
        var request = new AssignIssueRequest { UserId = Staff2Id, Note = "Vui lòng xử lý sớm" };

        // Act
        var result = await _service.AssignIssueAsync(UnassignedIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AssignedMembers.Should().ContainSingle(m => m.UserId == Staff2Id && m.Status == AssignmentMemberStatus.Pending);

        var memberInDb = await _context.IssueAssignmentMembers
            .FirstOrDefaultAsync(m => m.Assignment.IssueId == UnassignedIssueId && m.UserId == Staff2Id);
        memberInDb.Should().NotBeNull();
        memberInDb!.Status.Should().Be(AssignmentMemberStatus.Pending);
        memberInDb.AssignedBy.Should().Be(ManagerId);
        memberInDb.Note.Should().Be("Vui lòng xử lý sớm");

        var updateInDb = await _context.IssueUpdates
            .FirstOrDefaultAsync(u => u.IssueId == UnassignedIssueId && u.CreatedBy == ManagerId);
        updateInDb.Should().NotBeNull();
        updateInDb!.Note.Should().Contain("Da gan nhan vien 'Nhân viên A2'");

        var notificationInDb = await _context.Notifications.FirstOrDefaultAsync(n => n.UserId == Staff2Id && n.IssueId == UnassignedIssueId);
        notificationInDb.Should().NotBeNull();
        notificationInDb!.Title.Should().Be("Ban duoc gan xu ly su co");
    }

    [Fact]
    public async Task AssignIssueAsync_NonExistentIssue_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new AssignIssueRequest { UserId = Staff1Id };
        long nonExistentIssueId = 999;

        // Act
        Func<Task> act = () => _service.AssignIssueAsync(nonExistentIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*ID = {nonExistentIssueId}*");
    }

    [Fact]
    public async Task AssignIssueAsync_NonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new AssignIssueRequest { UserId = "non-existent-user" };

        // Act
        Func<Task> act = () => _service.AssignIssueAsync(UnassignedIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*ID = non-existent-user*");
    }

    [Fact]
    public async Task AssignIssueAsync_UserAlreadyAssigned_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new AssignIssueRequest { UserId = Staff1Id }; // Staff1 is already assigned to issue 102

        // Act
        Func<Task> act = () => _service.AssignIssueAsync(AssignedIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*da duoc gan vao phan cong nay*");
    }

    #endregion

    #region UpdateIssueStatusAsync Tests

    [Fact]
    public async Task UpdateIssueStatusAsync_ToResolved_UpdatesStatusAndResolvedAt()
    {
        // Arrange
        var request = new DepartmentManagerUpdateIssueStatusRequest
        {
            StatusId = StatusResolvedId,
            Note = "Đã xác nhận hoàn thành."
        };

        // Act
        var result = await _service.UpdateIssueStatusAsync(AssignedIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StatusName.Should().Be("Đã giải quyết");
        result.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var issueInDb = await _context.Issues.FindAsync(AssignedIssueId);
        issueInDb.Should().NotBeNull();
        issueInDb!.StatusId.Should().Be(StatusResolvedId);
        issueInDb.ResolvedAt.Should().NotBeNull();

        var updateInDb = await _context.IssueUpdates.OrderByDescending(u => u.CreatedAt).FirstAsync(u => u.IssueId == AssignedIssueId);
        updateInDb.ToStatusId.Should().Be(StatusResolvedId);
        updateInDb.Note.Should().Be("Đã xác nhận hoàn thành.");
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_ToClosedWhenNotResolved_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new DepartmentManagerUpdateIssueStatusRequest { StatusId = StatusClosedId };

        // Act
        Func<Task> act = () => _service.UpdateIssueStatusAsync(AssignedIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*phải chuyển sang RESOLVED trước khi CLOSED*");
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_ToClosedWhenAlreadyResolved_UpdatesStatusAndClosedAt()
    {
        // Arrange
        var request = new DepartmentManagerUpdateIssueStatusRequest { StatusId = StatusClosedId, Note = "Đóng và lưu trữ." };

        // Act
        var result = await _service.UpdateIssueStatusAsync(ResolvedIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StatusName.Should().Be("Đã đóng");

        var issueInDb = await _context.Issues.FindAsync(ResolvedIssueId);
        issueInDb.Should().NotBeNull();
        issueInDb!.StatusId.Should().Be(StatusClosedId);
        issueInDb.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateIssueStatusAsync_NonExistentIssue_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new DepartmentManagerUpdateIssueStatusRequest { StatusId = StatusResolvedId };
        long nonExistentIssueId = 999;

        // Act
        Func<Task> act = () => _service.UpdateIssueStatusAsync(nonExistentIssueId, request, ManagerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*ID = {nonExistentIssueId}*");
    }

    #endregion

    #region GetIssueDetailAsync Tests

    [Fact]
    public async Task GetIssueDetailAsync_ExistingIssue_ReturnsCorrectDetails()
    {
        // Arrange
        // Act
        var result = await _service.GetIssueDetailAsync(AssignedIssueId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IssueId.Should().Be(AssignedIssueId);
        result.Title.Should().Be("Assigned Water Issue");
        result.CurrentAssignment.Should().NotBeNull();
        result.CurrentAssignment!.DepartmentId.Should().Be(Dept1Id);
        result.AssignedMembers.Should().ContainSingle().Which.UserId.Should().Be(Staff1Id);
    }

    [Fact]
    public async Task GetIssueDetailAsync_NonExistentIssue_ReturnsNull()
    {
        // Arrange
        long nonExistentIssueId = 999;

        // Act
        var result = await _service.GetIssueDetailAsync(nonExistentIssueId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}