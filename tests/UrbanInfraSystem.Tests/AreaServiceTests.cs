using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Areas;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class AreaServiceTests
{
    /// <summary>
    /// Helper để tạo dữ liệu mẫu cho các test case.
    /// </summary>
    private static async Task SeedDataAsync(Infrastructure.Persistence.AppDbContext context)
    {
        var areas = new List<Area>
        {
            new() { AreaId = 1, AreaCode = "HN", AreaName = "Thành phố Hà Nội", AreaType = "City", IsActive = true },
            new() { AreaId = 2, AreaCode = "HN-BD", AreaName = "Quận Ba Đình", AreaType = "District", ParentAreaId = 1, IsActive = true },
            new() { AreaId = 3, AreaCode = "HN-HK", AreaName = "Quận Hoàn Kiếm", AreaType = "District", ParentAreaId = 1, IsActive = true },
            new() { AreaId = 4, AreaCode = "HN-CG", AreaName = "Quận Cầu Giấy", AreaType = "District", ParentAreaId = 1, IsActive = false },
            new() { AreaId = 5, AreaCode = "HCM", AreaName = "Thành phố Hồ Chí Minh", AreaType = "City", IsActive = true }
        };
        context.Areas.AddRange(areas);
        await context.SaveChangesAsync();
    }

    #region GetAreasAsync Tests

    [Fact]
    public async Task GetAreasAsync_ShouldReturnAllAreas_WhenNoFilterIsApplied()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var filter = new AreaFilterRequest();

        // Act
        var result = await service.GetAreasAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAreasAsync_ShouldFilterByParentAreaId()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var filter = new AreaFilterRequest { ParentAreaId = 1 };

        // Act
        var result = await service.GetAreasAsync(filter);

        // Assert
        result.Should().HaveCount(3);
        result.Select(a => a.AreaCode).Should().BeEquivalentTo("HN-BD", "HN-HK", "HN-CG");
    }

    [Fact]
    public async Task GetAreasAsync_ShouldFilterByAreaType()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var filter = new AreaFilterRequest { AreaType = "City" };

        // Act
        var result = await service.GetAreasAsync(filter);

        // Assert
        result.Should().HaveCount(2);
        result.Select(a => a.AreaCode).Should().BeEquivalentTo("HN", "HCM");
    }

    [Fact]
    public async Task GetAreasAsync_ShouldFilterByIsActive()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var filter = new AreaFilterRequest { IsActive = false };

        // Act
        var result = await service.GetAreasAsync(filter);

        // Assert
        var area = result.Should().ContainSingle().Subject;
        area.AreaCode.Should().Be("HN-CG");
    }

    [Fact]
    public async Task GetAreasAsync_ShouldFilterBySearchKeyword()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var filter = new AreaFilterRequest { Search = "Hoàn" };

        // Act
        var result = await service.GetAreasAsync(filter);

        // Assert
        var area = result.Should().ContainSingle().Subject;
        area.AreaCode.Should().Be("HN-HK");
    }

    #endregion

    #region GetAreaByIdAsync Tests

    [Fact]
    public async Task GetAreaByIdAsync_ShouldReturnAreaWithSubAreas_WhenIdExists()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);

        // Act
        var result = await service.GetAreaByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.AreaCode.Should().Be("HN");
        result.SubAreas.Should().NotBeNull().And.HaveCount(3);
        result.SubAreas!.Select(sa => sa.AreaCode).Should().Contain(["HN-BD", "HN-HK", "HN-CG"]);
    }

    [Fact]
    public async Task GetAreaByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);

        // Act
        var result = await service.GetAreaByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAreaAsync Tests

    [Fact]
    public async Task CreateAreaAsync_ShouldCreateRootAreaSuccessfully()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);
        var request = new CreateAreaRequest
        {
            AreaCode = "DN",
            AreaName = "  Thành phố Đà Nẵng  ", // Test trimming
            AreaType = "City",
            IsActive = true
        };

        // Act
        var result = await service.CreateAreaAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AreaCode.Should().Be("DN");
        result.AreaName.Should().Be("Thành phố Đà Nẵng");
        result.ParentAreaId.Should().BeNull();

        var areaInDb = await db.Areas.FirstOrDefaultAsync(a => a.AreaCode == "DN");
        areaInDb.Should().NotBeNull();
        areaInDb!.AreaName.Should().Be("Thành phố Đà Nẵng");
    }

    [Fact]
    public async Task CreateAreaAsync_ShouldThrowInvalidOperationException_WhenAreaCodeExists()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var request = new CreateAreaRequest
        {
            AreaCode = "HN", // Existing code
            AreaName = "Tên Mới",
            AreaType = "City"
        };

        // Act
        Func<Task> act = () => service.CreateAreaAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã tồn tại*");
    }

    [Fact]
    public async Task CreateAreaAsync_ShouldThrowArgumentException_WhenParentAreaIdDoesNotExist()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);
        var request = new CreateAreaRequest
        {
            ParentAreaId = 999, // Non-existent parent
            AreaCode = "NEW-DISTRICT",
            AreaName = "Quận Mới",
            AreaType = "District"
        };

        // Act
        Func<Task> act = () => service.CreateAreaAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*không tồn tại*");
    }

    [Fact]
    public async Task CreateAreaAsync_ShouldCalculateCentroid_WhenBoundaryIsProvidedAndCentroidIsNull()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);
        var geoJson = """{ "type": "Polygon", "coordinates": [ [ [105.80, 21.00], [105.85, 21.00], [105.85, 21.05], [105.80, 21.05], [105.80, 21.00] ] ] }""";
        var request = new CreateAreaRequest
        {
            AreaCode = "TEST-CENTROID",
            AreaName = "Test Centroid",
            AreaType = "Test",
            Boundary = JsonNode.Parse(geoJson),
            CentroidLatitude = null, // Explicitly null
            CentroidLongitude = null // Explicitly null
        };

        // Act
        var result = await service.CreateAreaAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CentroidLatitude.Should().NotBeNull().And.BeApproximately(21.025m, 0.0001m);
        result.CentroidLongitude.Should().NotBeNull().And.BeApproximately(105.825m, 0.0001m);
    }

    [Fact]
    public async Task CreateAreaAsync_ShouldThrowArgumentException_WhenBoundaryIsInvalidGeoJson()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);
        var invalidGeoJson = """{ "type": "InvalidShape", "coordinates": "wrong" }""";
        var request = new CreateAreaRequest
        {
            AreaCode = "INVALID-GEO",
            AreaName = "Invalid Geo",
            AreaType = "Test",
            Boundary = JsonNode.Parse(invalidGeoJson)
        };

        // Act
        Func<Task> act = () => service.CreateAreaAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*không hợp lệ*");
    }

    #endregion

    #region UpdateAreaAsync Tests

    [Fact]
    public async Task UpdateAreaAsync_ShouldUpdateAreaSuccessfully()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var request = new UpdateAreaRequest
        {
            AreaCode = "HN-HK-NEW",
            AreaName = "Quận Hoàn Kiếm Mới",
            AreaType = "District",
            IsActive = false,
            ParentAreaId = 5 // Change parent
        };

        // Act
        var result = await service.UpdateAreaAsync(3, request);

        // Assert
        result.Should().NotBeNull();
        result.AreaCode.Should().Be("HN-HK-NEW");
        result.AreaName.Should().Be("Quận Hoàn Kiếm Mới");
        result.IsActive.Should().BeFalse();
        result.ParentAreaId.Should().Be(5);

        var areaInDb = await db.Areas.FindAsync(3);
        areaInDb.Should().NotBeNull();
        areaInDb!.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAreaAsync_ShouldThrowKeyNotFoundException_WhenAreaDoesNotExist()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);
        var request = new UpdateAreaRequest { AreaCode = "DNE", AreaName = "Does Not Exist", AreaType = "Test" };

        // Act
        Func<Task> act = () => service.UpdateAreaAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAreaAsync_ShouldThrowArgumentException_WhenSettingSelfAsParent()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var request = new UpdateAreaRequest
        {
            ParentAreaId = 2, // Self
            AreaCode = "HN-BD",
            AreaName = "Quận Ba Đình",
            AreaType = "District"
        };

        // Act
        Func<Task> act = () => service.UpdateAreaAsync(2, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*tự nhận chính mình*");
    }

    [Fact]
    public async Task UpdateAreaAsync_ShouldThrowInvalidOperationException_WhenUpdatingToExistingCode()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);
        var request = new UpdateAreaRequest
        {
            AreaCode = "HN-HK", // Code of area with ID 3
            AreaName = "Quận Ba Đình",
            AreaType = "District"
        };

        // Act
        Func<Task> act = () => service.UpdateAreaAsync(2, request); // Update area 2 with code of area 3

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã tồn tại ở khu vực khác*");
    }

    #endregion

    #region DeleteAreaAsync Tests

    [Fact]
    public async Task DeleteAreaAsync_ShouldDeleteAreaWithoutSubAreasSuccessfully()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);

        // Act
        var result = await service.DeleteAreaAsync(5); // HCM has no sub-areas

        // Assert
        result.Should().BeTrue();
        var areaInDb = await db.Areas.FindAsync(5);
        areaInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAreaAsync_ShouldReturnFalse_WhenAreaDoesNotExist()
    {
        // Arrange
        await using var db = TestDb.Create();
        var service = new AreaService(db);

        // Act
        var result = await service.DeleteAreaAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAreaAsync_ShouldThrowInvalidOperationException_WhenAreaHasSubAreas()
    {
        // Arrange
        await using var db = TestDb.Create();
        await SeedDataAsync(db);
        var service = new AreaService(db);

        // Act
        Func<Task> act = () => service.DeleteAreaAsync(1); // HN has sub-areas

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*khu vực con phụ thuộc*");
    }

    #endregion
}