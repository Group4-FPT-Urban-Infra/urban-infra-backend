using Microsoft.Extensions.Logging.Abstractions;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueTests
{
    [Fact]
    public async Task SearchIssuesAsync_ReturnsOnlyPublicNonArchivedIssues_MatchingFilters()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        db.Issues.AddRange(
            NewIssue(1, "ISS-001", "Ổ gà lớn", isPublic: true, isArchived: false),
            NewIssue(2, "ISS-002", "Ổ gà đã ẩn", isPublic: true, isArchived: true),
            NewIssue(3, "ISS-003", "Sự cố nội bộ", isPublic: false, isArchived: false));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.SearchIssuesAsync(new SearchIssuesRequest
        {
            Keyword = "ổ gà",
            StatusCodes = ["OPEN"],
            PriorityCodes = ["HIGH"],
            Page = 1,
            PageSize = 10
        });

        Assert.True(result.Success);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal(1, item.Id);
        Assert.Equal("ISS-001", item.PublicCode);
        Assert.Equal("POTHOLE", item.IssueType.Code);
        Assert.Equal(1, result.Data.TotalItems);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ReturnsLookupsAndCurrentDepartment()
    {
        await using var db = TestDb.Create();
        SeedLookups(db);
        db.Departments.Add(new Department
        {
            DepartmentId = 9,
            DepartmentCode = "ROAD",
            DepartmentName = "Phòng quản lý đường bộ"
        });
        db.Issues.Add(NewIssue(10, "ISS-010", "Mặt đường hư hỏng"));
        db.IssueAssignments.Add(new IssueAssignment
        {
            AssignmentId = 20,
            IssueId = 10,
            DepartmentId = 9,
            AssignmentMethod = "MANUAL",
            AssignedAt = DateTime.UtcNow,
            IsCurrent = true
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetIssueByIdAsync(10);

        Assert.True(result.Success);
        Assert.Equal("Ổ gà", result.Data!.IssueType.Name);
        Assert.Equal("Mức cao", result.Data.Priority.Name);
        Assert.Equal("Đang mở", result.Data.Status.Name);
        Assert.Equal("Phòng quản lý đường bộ", result.Data.CurrentDepartment!.Name);
    }

    [Fact]
    public async Task GetIssueByIdAsync_ReturnsFailure_WhenIssueDoesNotExist()
    {
        await using var db = TestDb.Create();

        var result = await CreateService(db).GetIssueByIdAsync(999);

        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Contains("999", result.Message);
    }

    [Fact]
    public async Task CreateIssueAsync_RejectsOtherTypeWithoutCustomDescription()
    {
        await using var db = TestDb.Create();
        db.Users.Add(new ApplicationUser
        {
            Id = "citizen-test",
            UserName = "citizen@test.local",
            Email = "citizen@test.local",
            FullName = "Citizen Test"
        });
        db.IssueTypes.Add(new IssueType
        {
            IssueTypeId = 99,
            TypeCode = "OTHER",
            TypeName = "Khác",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).CreateIssueAsync(new CreateIssueFormRequest
        {
            IssueTypeIds = [99],
            AreaId = 1,
            Title = "Sự cố chưa phân loại",
            Description = "Nội dung mô tả sự cố chưa phân loại",
            Latitude = 21.028m,
            Longitude = 105.834m
        }, "citizen-test");

        Assert.False(result.Success);
        Assert.Contains("OTHER", result.Message);
        Assert.Empty(db.Issues);
    }

    internal static void SeedLookups(Infrastructure.Persistence.AppDbContext db)
    {
        db.IssueTypes.Add(new IssueType
        {
            IssueTypeId = 1, TypeCode = "POTHOLE", TypeName = "Ổ gà", IsActive = true
        });
        db.Areas.Add(new Area
        {
            AreaId = 1, AreaCode = "HN", AreaName = "Hà Nội", AreaType = "CITY", IsActive = true
        });
        db.IssuePriorities.Add(new IssuePriority
        {
            PriorityId = 1, PriorityCode = "HIGH", PriorityName = "Mức cao", SeverityRank = 3
        });
        db.IssueStatuses.Add(new IssueStatus
        {
            StatusId = 1, StatusCode = "OPEN", StatusName = "Đang mở", DisplayOrder = 1
        });
    }

    internal static Issue NewIssue(
        long id,
        string code,
        string title,
        bool isPublic = true,
        bool isArchived = false) => new()
    {
        IssueId = id,
        ReportId = id,
        PublicCode = code,
        ReporterId = "citizen-test",
        IssueTypeId = 1,
        AreaId = 1,
        PriorityId = 1,
        StatusId = 1,
        Title = title,
        Description = $"Mô tả cho {title}",
        Latitude = 21.028m,
        Longitude = 105.834m,
        IsPublic = isPublic,
        IsArchived = isArchived,
        ReportedAt = DateTime.UtcNow
    };

    internal static IssueService CreateService(Infrastructure.Persistence.AppDbContext db) =>
        new(db, new TestWebHostEnvironment(), NullLogger<IssueService>.Instance);
}
