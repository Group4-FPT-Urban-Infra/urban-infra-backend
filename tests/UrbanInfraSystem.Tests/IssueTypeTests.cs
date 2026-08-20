using UrbanInfraSystem.Application.DTOs.IssueTypes;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class IssueTypeTests
{
    [Fact]
    public async Task CreateAsync_CreatesChildAndStoresAuditUser()
    {
        await using var db = TestDb.Create();
        db.IssueTypes.Add(new IssueType
        {
            IssueTypeId = 1, TypeCode = "ROAD", TypeName = "Đường bộ"
        });
        await db.SaveChangesAsync();
        var service = new IssueTypeService(db, new TestCurrentUser("admin-01"));

        var result = await service.CreateAsync(new CreateIssueTypeRequest
        {
            TypeCode = " pothole ",
            TypeName = " Ổ gà ",
            ParentIssueTypeId = 1,
            IsActive = true
        });

        Assert.True(result.Success);
        Assert.Equal("POTHOLE", result.Data!.TypeCode);
        Assert.Equal("Ổ gà", result.Data.TypeName);
        Assert.Equal(1, result.Data.ParentIssueTypeId);
        Assert.Equal("admin-01", result.Data.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenCodeAlreadyExists()
    {
        await using var db = TestDb.Create();
        db.IssueTypes.Add(new IssueType
        {
            IssueTypeId = 1, TypeCode = "ROAD", TypeName = "Đường bộ"
        });
        await db.SaveChangesAsync();

        var result = await new IssueTypeService(db, new TestCurrentUser()).CreateAsync(
            new CreateIssueTypeRequest { TypeCode = "ROAD", TypeName = "Trùng mã" });

        Assert.False(result.Success);
        Assert.Contains("đã tồn tại", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_RejectsCircularParentReference()
    {
        await using var db = TestDb.Create();
        db.IssueTypes.AddRange(
            new IssueType { IssueTypeId = 1, TypeCode = "ROOT", TypeName = "Gốc" },
            new IssueType { IssueTypeId = 2, TypeCode = "CHILD", TypeName = "Con", ParentIssueTypeId = 1 });
        await db.SaveChangesAsync();

        var result = await new IssueTypeService(db, new TestCurrentUser()).UpdateAsync(
            1, new UpdateIssueTypeRequest { ParentIssueTypeId = 2 });

        Assert.False(result.Success);
        Assert.Contains("vòng lặp", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_RejectsParentWithActiveChildren()
    {
        await using var db = TestDb.Create();
        db.IssueTypes.AddRange(
            new IssueType { IssueTypeId = 1, TypeCode = "ROOT", TypeName = "Gốc" },
            new IssueType { IssueTypeId = 2, TypeCode = "CHILD", TypeName = "Con", ParentIssueTypeId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var result = await new IssueTypeService(db, new TestCurrentUser()).DeleteAsync(1);

        Assert.False(result.Success);
        Assert.False((await db.IssueTypes.FindAsync(1))!.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesLeafType()
    {
        await using var db = TestDb.Create();
        db.IssueTypes.Add(new IssueType
        {
            IssueTypeId = 3, TypeCode = "LEAF", TypeName = "Lá", IsActive = true
        });
        await db.SaveChangesAsync();

        var result = await new IssueTypeService(db, new TestCurrentUser("admin-02")).DeleteAsync(3);
        var entity = await db.IssueTypes.FindAsync(3);

        Assert.True(result.Success);
        Assert.True(entity!.IsDeleted);
        Assert.False(entity.IsActive);
        Assert.Equal("admin-02", entity.UpdatedBy);
    }

    [Fact]
    public async Task SearchAsync_FiltersDeletedAndInactiveTypes()
    {
        await using var db = TestDb.Create();
        db.IssueTypes.AddRange(
            new IssueType { IssueTypeId = 1, TypeCode = "ROAD", TypeName = "Đường bộ", IsActive = true },
            new IssueType { IssueTypeId = 2, TypeCode = "LIGHT", TypeName = "Chiếu sáng", IsActive = false },
            new IssueType { IssueTypeId = 3, TypeCode = "OLD", TypeName = "Đã xóa", IsDeleted = true });
        await db.SaveChangesAsync();

        var result = await new IssueTypeService(db, new TestCurrentUser()).SearchAsync(
            new SearchIssueTypesRequest { IsActiveOnly = true, Page = 1, PageSize = 10 });

        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("ROAD", item.TypeCode);
    }
}
