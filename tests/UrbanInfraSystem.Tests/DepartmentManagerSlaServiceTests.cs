using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class DepartmentManagerSlaServiceTests
{
    private AppDbContext _context = null!;
    private DepartmentManagerSlaService _service = null!;

    private const int Dept1Id = 1;
    private const int Dept2Id = 2;
    private const int EmptyDeptId = 3;

    public DepartmentManagerSlaServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        _service = new DepartmentManagerSlaService(_context);
        SeedDataAsync().GetAwaiter().GetResult();
    }

    private async Task SeedDataAsync()
    {
        var now = DateTime.UtcNow;

        // 1. Departments
        var dept1 = new Department { DepartmentId = Dept1Id, DepartmentName = "Phòng Vận hành" };
        var dept2 = new Department { DepartmentId = Dept2Id, DepartmentName = "Phòng Kỹ thuật" };
        var emptyDept = new Department { DepartmentId = EmptyDeptId, DepartmentName = "Phòng Trống" };
        _context.Departments.AddRange(dept1, dept2, emptyDept);

        // 2. Lookups
        var prioHigh = new IssuePriority { PriorityId = 1, PriorityName = "Cao" };
        var statusNew = new IssueStatus { StatusId = 1, StatusName = "Mới" };
        _context.IssuePriorities.Add(prioHigh);
        _context.IssueStatuses.Add(statusNew);

        // 3. Issues & Assignments for Dept 1
        var issuesDept1 = new[]
        {
            new Issue { IssueId = 101L, Title = "Resolved On Time (This Month)", Priority = prioHigh, Status = statusNew },
            new Issue { IssueId = 102L, Title = "Breached, Resolved Late (This Month)", Priority = prioHigh, Status = statusNew },
            new Issue { IssueId = 103L, Title = "Breached, Not Resolved (This Month)", Priority = prioHigh, Status = statusNew },
            new Issue { IssueId = 104L, Title = "Near Deadline (Resolution)", Priority = prioHigh, Status = statusNew },
            new Issue { IssueId = 105L, Title = "Near Deadline (First Response)", Priority = prioHigh, Status = statusNew },
            new Issue { IssueId = 106L, Title = "Resolved On Time (Last Month)", Priority = prioHigh, Status = statusNew },
            new Issue { IssueId = 107L, Title = "Not Near Deadline", Priority = prioHigh, Status = statusNew }
        };
        _context.Issues.AddRange(issuesDept1);
        _context.IssueAssignments.AddRange(issuesDept1.Select(i => new IssueAssignment
        {
            IssueId = i.IssueId,
            DepartmentId = Dept1Id,
            IsCurrent = true,
            AssignedAt = now.AddDays(-1)
        }));

        // 4. SLAs for Dept 1
        _context.IssueSlas.AddRange(
            // This month
            new IssueSla { IssueId = 101L, CreatedAt = now.AddDays(-5), ResolutionDueAt = now.AddDays(-1), ResolvedAt = now.AddDays(-2) }, // Resolved on time
            new IssueSla { IssueId = 102L, CreatedAt = now.AddDays(-4), ResolutionDueAt = now.AddDays(-2), ResolvedAt = now.AddDays(-1) }, // Breached (resolved late)
            new IssueSla { IssueId = 103L, CreatedAt = now.AddDays(-3), ResolutionDueAt = now.AddDays(-1), ResolvedAt = null },             // Breached (deadline passed)
            new IssueSla { IssueId = 104L, CreatedAt = now.AddDays(-1), ResolutionDueAt = now.AddHours(1), ResolvedAt = null },             // Near deadline (resolution)
            new IssueSla { IssueId = 105L, CreatedAt = now.AddDays(-1), FirstResponseDueAt = now.AddMinutes(30), ResolutionDueAt = now.AddHours(10), ResolvedAt = null }, // Near deadline (first response)
            new IssueSla { IssueId = 107L, CreatedAt = now.AddDays(-1), ResolutionDueAt = now.AddHours(5), ResolvedAt = null },             // Not near deadline
            // Last month
            new IssueSla { IssueId = 106L, CreatedAt = now.AddMonths(-1), ResolutionDueAt = now.AddMonths(-1).AddDays(5), ResolvedAt = now.AddMonths(-1).AddDays(4) } // Resolved on time last month
        );

        // 5. Issue & Assignment & SLA for Dept 2 (to test partitioning)
        var issueDept2 = new Issue { IssueId = 201L, Title = "Dept 2 Issue", Priority = prioHigh, Status = statusNew };
        _context.Issues.Add(issueDept2);
        _context.IssueAssignments.Add(new IssueAssignment { IssueId = 201L, DepartmentId = Dept2Id, IsCurrent = true, AssignedAt = now.AddDays(-1) });
        _context.IssueSlas.Add(new IssueSla { IssueId = 201L, CreatedAt = now.AddDays(-1), ResolutionDueAt = now.AddHours(1), ResolvedAt = null });

        await _context.SaveChangesAsync();
    }

    #region GetOverviewAsync Tests

    [Fact]
    public async Task GetOverviewAsync_WithDataInCurrentMonth_ReturnsCorrectStatsAndPartitionsData()
    {
        // Act
        var result = await _service.GetOverviewAsync(Dept1Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalIssuesInMonth.Should().Be(5, "vì có 5 sự cố có SLA được tạo trong tháng này cho phòng ban 1");
        result.ResolvedOnTime.Should().Be(1, "vì chỉ có sự cố 101 được giải quyết đúng hạn");
        result.Breached.Should().Be(2, "vì sự cố 102 giải quyết trễ và 103 đã qua hạn mà chưa giải quyết");
        result.ResolutionRate.Should().Be(20.0, "vì (1/5)*100");
        result.AvgResponseTimeMinutes.Should().Be(45, "vì là giá trị hardcode trong service");
        result.AvgResolutionTimeMinutes.Should().Be(180, "vì là giá trị hardcode trong service");
    }

    [Fact]
    public async Task GetOverviewAsync_ForDepartmentWithNoIssues_ReturnsZeroStats()
    {
        // Act
        var result = await _service.GetOverviewAsync(EmptyDeptId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalIssuesInMonth.Should().Be(0);
        result.ResolvedOnTime.Should().Be(0);
        result.Breached.Should().Be(0);
        result.ResolutionRate.Should().Be(0);
    }

    #endregion

    #region GetNearDeadlineAsync Tests

    [Fact]
    public async Task GetNearDeadlineAsync_WithIssuesNearingDeadline_ReturnsCorrectSortedList()
    {
        // Act
        var result = await _service.GetNearDeadlineAsync(Dept1Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Select(i => i.IssueId).Should().NotContain(201L, "vì sự cố này thuộc phòng ban khác");

        var issueIds = result.Select(i => i.IssueId).ToList();
        issueIds.Should().ContainInOrder(104L, 105L);
        var item104 = result.First(i => i.IssueId == 104L);
        item104.DeadlineType.Should().Be("Resolution");
        item104.MinutesRemaining.Should().BeInRange(55, 60);

        var item105 = result.First(i => i.IssueId == 105L);
        item105.DeadlineType.Should().Be("First Response");
        item105.MinutesRemaining.Should().BeInRange(25, 30);
    }

    [Fact]
    public async Task GetNearDeadlineAsync_WithNoIssuesNearingDeadline_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetNearDeadlineAsync(EmptyDeptId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_WithDataSpanningMonths_ReturnsCorrectMonthlyBreakdown()
    {
        // Act
        var result = await _service.GetHistoryAsync(Dept1Id, 2, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);

        // Current Month
        var currentMonth = DateTime.UtcNow;
        var currentMonthHistory = result.Items.FirstOrDefault(i => i.Year == currentMonth.Year && i.Month == currentMonth.Month);
        currentMonthHistory.Should().NotBeNull();
        currentMonthHistory!.TotalIssues.Should().Be(5, "vì có 5 SLA được tạo trong tháng này");
        currentMonthHistory.ResolvedOnTime.Should().Be(1, "vì chỉ có issue 101");
        currentMonthHistory.Breached.Should().Be(2, "vì có issue 102 và 103");
        currentMonthHistory.ResolutionRate.Should().Be(20.0);

        // Last Month
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var lastMonthHistory = result.Items.FirstOrDefault(i => i.Year == lastMonth.Year && i.Month == lastMonth.Month);
        lastMonthHistory.Should().NotBeNull();
        lastMonthHistory!.TotalIssues.Should().Be(1, "vì có 1 SLA được tạo trong tháng trước");
        lastMonthHistory.ResolvedOnTime.Should().Be(1, "vì issue 106 được giải quyết đúng hạn");
        lastMonthHistory.Breached.Should().Be(0);
        lastMonthHistory.ResolutionRate.Should().Be(100.0);
    }

    [Fact]
    public async Task GetHistoryAsync_ForDepartmentWithNoHistory_ReturnsEmptyItems()
    {
        // Act
        var result = await _service.GetHistoryAsync(EmptyDeptId, 6, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(6);
        result.Items.Should().AllSatisfy(item =>
        {
            item.TotalIssues.Should().Be(0);
            item.ResolvedOnTime.Should().Be(0);
            item.Breached.Should().Be(0);
            item.ResolutionRate.Should().Be(0);
        });
    }

    #endregion
}