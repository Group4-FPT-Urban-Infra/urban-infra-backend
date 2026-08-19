using System;
using System.Collections.Generic;
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

public class DepartmentManagerDashboardServiceTests
{
    private AppDbContext _context;
    private DepartmentManagerDashboardService _service;

    private const int Dept1Id = 1;
    private const int Dept2Id = 2;
    private const int EmptyDeptId = 3;

    public DepartmentManagerDashboardServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        _service = new DepartmentManagerDashboardService(_context);
        SeedDataAsync().GetAwaiter().GetResult();
    }

    private async Task SeedDataAsync()
    {
        // Lookups
        var dept1 = new Department { DepartmentId = Dept1Id, DepartmentName = "Phòng Vận hành", IsActive = true };
        var dept2 = new Department { DepartmentId = Dept2Id, DepartmentName = "Phòng Kỹ thuật", IsActive = true };
        var emptyDept = new Department { DepartmentId = EmptyDeptId, DepartmentName = "Phòng Trống", IsActive = true };
        _context.Departments.AddRange(dept1, dept2, emptyDept);

        var user1 = new ApplicationUser { Id = "staff-1", FullName = "Nhân viên A", IsActive = true };
        var user2 = new ApplicationUser { Id = "staff-2", FullName = "Nhân viên B", IsActive = true };
        _context.Users.AddRange(user1, user2);

        _context.DepartmentMembers.AddRange(
            new DepartmentMember { DepartmentId = Dept1Id, UserId = "staff-1", IsActive = true },
            new DepartmentMember { DepartmentId = Dept1Id, UserId = "staff-2", IsActive = true }
        );

        var type1 = new IssueType { IssueTypeId = 1, TypeName = "Sự cố điện" };
        _context.IssueTypes.Add(type1);

        var prioHigh = new IssuePriority { PriorityId = 2, PriorityName = "Cao", SeverityRank = 1 };
        var prioLow = new IssuePriority { PriorityId = 1, PriorityName = "Thấp", SeverityRank = 2 };
        _context.IssuePriorities.AddRange(prioHigh, prioLow);

        var statusNew = new IssueStatus { StatusId = 1, StatusName = "Mới" };
        _context.IssueStatuses.Add(statusNew);

        var now = DateTime.UtcNow;

        // Issues for Department 1
        var issue101 = new Issue { IssueId = 101, Title = "Unassigned 1 (High Prio)", PriorityId = 2, ReportedAt = now.AddDays(-2), IssueType = type1, Priority = prioHigh, Status = statusNew };
        var issue102 = new Issue { IssueId = 102, Title = "Processing by Staff 1", PriorityId = 1, ReportedAt = now.AddDays(-1), IssueType = type1, Priority = prioLow, Status = statusNew };
        var issue103 = new Issue { IssueId = 103, Title = "SLA Risk", PriorityId = 2, ReportedAt = now.AddDays(-3), IssueType = type1, Priority = prioHigh, Status = statusNew };
        var issue104 = new Issue { IssueId = 104, Title = "Unassigned 2 (Low Prio)", PriorityId = 1, ReportedAt = now.AddHours(-5), IssueType = type1, Priority = prioLow, Status = statusNew };
        var issue105 = new Issue { IssueId = 105, Title = "Completed by Staff 2", PriorityId = 1, ReportedAt = now.AddDays(-4), IssueType = type1, Priority = prioLow, Status = statusNew };
        _context.Issues.AddRange(issue101, issue102, issue103, issue104, issue105);

        // Issue for Department 2 (to test data partitioning)
        var issue201 = new Issue { IssueId = 201, Title = "Dept 2 Issue", PriorityId = 1, ReportedAt = now.AddDays(-1), IssueType = type1, Priority = prioLow, Status = statusNew };
        _context.Issues.Add(issue201);

        // Assignments
        var assignment101 = new IssueAssignment { AssignmentId = 1, IssueId = 101, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment102 = new IssueAssignment { AssignmentId = 2, IssueId = 102, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment103 = new IssueAssignment { AssignmentId = 3, IssueId = 103, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment104 = new IssueAssignment { AssignmentId = 4, IssueId = 104, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment105 = new IssueAssignment { AssignmentId = 5, IssueId = 105, DepartmentId = Dept1Id, IsCurrent = true };
        var assignment201 = new IssueAssignment { AssignmentId = 6, IssueId = 201, DepartmentId = Dept2Id, IsCurrent = true };
        _context.IssueAssignments.AddRange(assignment101, assignment102, assignment103, assignment104, assignment105, assignment201);

        // Assignment Members
        _context.IssueAssignmentMembers.AddRange(
            new IssueAssignmentMember { AssignmentId = 2, UserId = "staff-1", Status = AssignmentMemberStatus.Accepted }, // Processing
            new IssueAssignmentMember { AssignmentId = 5, UserId = "staff-2", Status = AssignmentMemberStatus.Completed } // Completed
        );

        // SLA
        _context.IssueSlas.Add(new IssueSla { IssueId = 103, ResolutionDueAt = now.AddHours(1) }); // At risk

        await _context.SaveChangesAsync();
    }

    #region GetStatsAsync Tests

    [Fact]
    public async Task GetStatsAsync_WithVariousIssueStates_ReturnsCorrectCountsAndPartitionsData()
    {
        // Arrange
        // Data is seeded in Setup() for Dept1Id = 1
        // - Unassigned: issue101, issue103, issue104 (3 total)
        // - Processing: issue102 (1 total, status Accepted)
        // - Completed: issue105 (not counted in stats)
        // - SLA Risk: issue103 (1 total, due in 1 hour)
        // - Dept 2 issue (issue201) should be ignored

        // Act
        var result = await _service.GetStatsAsync(Dept1Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UnassignedCount.Should().Be(3, "vì có 3 sự cố được phân về phòng nhưng chưa có cán bộ nào nhận");
        result.ProcessingCount.Should().Be(1, "vì có 1 sự cố đã được cán bộ chấp nhận");
        result.SlaBreachRisksCount.Should().Be(1, "vì có 1 sự cố có hạn giải quyết trong vòng 2 giờ tới");
        result.AvgResponseTimeMinutes.Should().Be(42, "vì giá trị này đang được hardcode trong service");
    }

    [Fact]
    public async Task GetStatsAsync_ForDepartmentWithNoIssues_ReturnsZeroCounts()
    {
        // Arrange
        // EmptyDeptId = 3 is seeded with no issues

        // Act
        var result = await _service.GetStatsAsync(EmptyDeptId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UnassignedCount.Should().Be(0);
        result.ProcessingCount.Should().Be(0);
        result.SlaBreachRisksCount.Should().Be(0);
        result.AvgResponseTimeMinutes.Should().Be(42); // Hardcoded value
    }

    #endregion

    #region GetTeamWorkloadAsync Tests

    [Fact]
    public async Task GetTeamWorkloadAsync_WithAssignedAndResolvedIssues_ReturnsCorrectWorkload()
    {
        // Arrange
        // Dept 1 has staff-1 and staff-2
        // - staff-1 is assigned issue102 (Accepted)
        // - staff-2 is assigned issue105 (Completed)

        // Act
        var result = await _service.GetTeamWorkloadAsync(Dept1Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Members.Should().HaveCount(2);

        var staff1Workload = result.Members.FirstOrDefault(m => m.UserId == "staff-1");
        staff1Workload.Should().NotBeNull();
        staff1Workload!.FullName.Should().Be("Nhân viên A");
        staff1Workload.AssignedCount.Should().Be(1, "vì staff-1 được gán 1 sự cố");
        staff1Workload.ResolvedCount.Should().Be(0, "vì sự cố của staff-1 chưa hoàn thành");

        var staff2Workload = result.Members.FirstOrDefault(m => m.UserId == "staff-2");
        staff2Workload.Should().NotBeNull();
        staff2Workload!.FullName.Should().Be("Nhân viên B");
        staff2Workload.AssignedCount.Should().Be(1, "vì staff-2 được gán 1 sự cố");
        staff2Workload.ResolvedCount.Should().Be(1, "vì sự cố của staff-2 đã hoàn thành");
    }

    [Fact]
    public async Task GetTeamWorkloadAsync_ForDepartmentWithNoMembers_ReturnsEmptyList()
    {
        // Arrange
        // Dept 2 has no members seeded

        // Act
        var result = await _service.GetTeamWorkloadAsync(Dept2Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Members.Should().BeEmpty();
    }

    #endregion

    #region GetUnassignedIssuesAsync Tests

    [Fact]
    public async Task GetUnassignedIssuesAsync_WithUnassignedIssues_ReturnsCorrectPagedAndSortedList()
    {
        // Arrange
        // Unassigned issues for Dept 1:
        // - issue101 (PrioId=2, High, older)
        // - issue103 (PrioId=2, High, oldest)
        // - issue104 (PrioId=1, Low, newer)
        // Expected order: by PriorityId DESC, then by ReportedAt ASC
        // So: issue103, issue101, issue104

        // Act
        var result = await _service.GetUnassignedIssuesAsync(Dept1Id, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var issueIds = result.Select(i => i.IssueId).ToList();
        issueIds.Should().ContainInOrder(new[] { 103L, 101L, 104L }, "vì phải sắp xếp theo độ ưu tiên giảm dần, sau đó theo thời gian báo cáo tăng dần");
        var firstIssue = result.First();
        firstIssue.IssueId.Should().Be(103);
        firstIssue.Title.Should().Be("SLA Risk");
        firstIssue.PriorityName.Should().Be("Cao");
        firstIssue.PriorityColor.Should().Be("#F44336"); // Rank 1
        firstIssue.SlaStatus.Should().Be("AT_RISK");
    }

    [Fact]
    public async Task GetUnassignedIssuesAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        // Same data as previous test, expected order: 103, 101, 104

        // Act
        var result = await _service.GetUnassignedIssuesAsync(Dept1Id, 2, 2, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().IssueId.Should().Be(104L, "vì trang 2, kích thước 2 sẽ lấy phần tử thứ 3");
    }

    [Fact]
    public async Task GetUnassignedIssuesAsync_WhenAllIssuesAreAssigned_ReturnsEmptyList()
    {
        // Arrange
        // Create a new department and assign all its issues
        var tempDeptId = 4;
        var tempUserId = "staff-temp";
        _context.Departments.Add(new Department { DepartmentId = tempDeptId, IsActive = true });
        _context.Users.Add(new ApplicationUser { Id = tempUserId, IsActive = true });
        _context.DepartmentMembers.Add(new DepartmentMember { DepartmentId = tempDeptId, UserId = tempUserId, IsActive = true });
        _context.Issues.Add(new Issue { IssueId = 401, Title = "Temp Issue" });
        var assignment = new IssueAssignment { AssignmentId = 401, IssueId = 401, DepartmentId = tempDeptId, IsCurrent = true };
        _context.IssueAssignments.Add(assignment);
        _context.IssueAssignmentMembers.Add(new IssueAssignmentMember { AssignmentId = 401, UserId = tempUserId, Status = AssignmentMemberStatus.Accepted });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUnassignedIssuesAsync(tempDeptId, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion
}