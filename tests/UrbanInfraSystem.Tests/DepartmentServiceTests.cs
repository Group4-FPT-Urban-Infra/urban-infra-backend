using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.Infrastructure.Services;
using Xunit;

namespace UrbanInfraSystem.Tests;

public class DepartmentServiceTests
{
    private AppDbContext _context;
    private DepartmentService _departmentService;

    private const int RootDeptId = 1;
    private const int ChildDeptId = 2;
    private const int InactiveDeptId = 3;
    private const int LeafDeptId = 4;
    private const int NonExistentDeptId = 999;

    public DepartmentServiceTests()
    {
        Setup();
    }

    private void Setup()
    {
        _context = TestDb.Create();
        _departmentService = new DepartmentService(_context);
        SeedData();
    }

    private void SeedData()
    {
        var rootDept = new Department { DepartmentId = RootDeptId, DepartmentCode = "ROOT", DepartmentName = "Sở Giao thông Vận tải", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var childDept = new Department { DepartmentId = ChildDeptId, ParentDepartmentId = RootDeptId, DepartmentCode = "CHILD", DepartmentName = "Phòng Quản lý Hạ tầng", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-9) };
        var inactiveDept = new Department { DepartmentId = InactiveDeptId, ParentDepartmentId = RootDeptId, DepartmentCode = "INACTIVE", DepartmentName = "Phòng Thanh tra (cũ)", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-20) };
        var leafDept = new Department { DepartmentId = LeafDeptId, DepartmentCode = "LEAF", DepartmentName = "Đơn vị độc lập", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) };
        _context.Departments.AddRange(rootDept, childDept, inactiveDept, leafDept);

        var managerUser = new ApplicationUser { Id = "manager-1", FullName = "Nguyễn Văn A", Email = "manager.a@email.com" };
        var staffUser = new ApplicationUser { Id = "staff-1", FullName = "Trần Thị B", Email = "staff.b@email.com" };
        _context.Users.AddRange(managerUser, staffUser);

        _context.DepartmentMembers.Add(new DepartmentMember { DepartmentId = RootDeptId, UserId = managerUser.Id, IsManager = true, IsActive = true });
        _context.DepartmentMembers.Add(new DepartmentMember { DepartmentId = ChildDeptId, UserId = staffUser.Id, IsManager = false, IsActive = true });

        _context.SaveChanges();
    }

    #region Get Tests

    [Fact]
    public async Task GetDepartmentsAsync_WithFilters_ReturnsFilteredList()
    {
        // Arrange
        // Act
        var byParent = await _departmentService.GetDepartmentsAsync(new DepartmentFilterRequest { ParentDepartmentId = RootDeptId });
        var byActive = await _departmentService.GetDepartmentsAsync(new DepartmentFilterRequest { IsActive = false });
        var bySearchCode = await _departmentService.GetDepartmentsAsync(new DepartmentFilterRequest { Search = "CHILD" });
        var bySearchName = await _departmentService.GetDepartmentsAsync(new DepartmentFilterRequest { Search = "Hạ tầng" });

        // Assert
        byParent.Should().HaveCount(2);
        byParent.Select(d => d.DepartmentCode).Should().BeEquivalentTo("CHILD", "INACTIVE");

        byActive.Should().ContainSingle().Which.DepartmentCode.Should().Be("INACTIVE");

        bySearchCode.Should().ContainSingle().Which.DepartmentId.Should().Be(ChildDeptId);
        bySearchName.Should().ContainSingle().Which.DepartmentId.Should().Be(ChildDeptId);
    }

    [Fact]
    public async Task GetDepartmentByIdAsync_ExistingId_ReturnsDepartmentWithChildrenAndManager()
    {
        // Arrange & Act
        var result = await _departmentService.GetDepartmentByIdAsync(RootDeptId);

        // Assert
        result.Should().NotBeNull();
        result!.DepartmentId.Should().Be(RootDeptId);
        result.ChildDepartments.Should().HaveCount(2);
        result.ChildDepartments.Select(d => d.DepartmentCode).Should().BeEquivalentTo("CHILD", "INACTIVE");
        result.ManagerId.Should().Be("manager-1");
        result.ManagerFullName.Should().Be("Nguyễn Văn A");
    }

    [Fact]
    public async Task GetDepartmentByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange & Act
        var result = await _departmentService.GetDepartmentByIdAsync(NonExistentDeptId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task CreateDepartmentAsync_ValidRootDepartment_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            DepartmentCode = "  NEW_ROOT  ",
            DepartmentName = "  Sở Tài nguyên Môi trường  ",
            IsActive = true
        };

        // Act
        var result = await _departmentService.CreateDepartmentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.DepartmentCode.Should().Be("NEW_ROOT");
        result.DepartmentName.Should().Be("Sở Tài nguyên Môi trường");
        result.ParentDepartmentId.Should().BeNull();

        var inDb = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "NEW_ROOT");
        inDb.Should().NotBeNull();
        inDb!.DepartmentName.Should().Be("Sở Tài nguyên Môi trường");
    }

    [Fact]
    public async Task CreateDepartmentAsync_ValidChildDepartment_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            ParentDepartmentId = RootDeptId,
            DepartmentCode = "NEW_CHILD",
            DepartmentName = "Phòng Kế hoạch",
            IsActive = true
        };

        // Act
        var result = await _departmentService.CreateDepartmentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ParentDepartmentId.Should().Be(RootDeptId);
        result.DepartmentCode.Should().Be("NEW_CHILD");
    }

    [Fact]
    public async Task CreateDepartmentAsync_NonExistentParentId_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            ParentDepartmentId = NonExistentDeptId,
            DepartmentCode = "FAIL",
            DepartmentName = "Fail Dept"
        };

        // Act
        Func<Task> act = async () => await _departmentService.CreateDepartmentAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*Đơn vị cha có ID = {NonExistentDeptId} không tồn tại*");
    }

    [Fact]
    public async Task CreateDepartmentAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateDepartmentRequest
        {
            DepartmentCode = "ROOT", // Existing code
            DepartmentName = "Duplicate Dept"
        };

        // Act
        Func<Task> act = async () => await _departmentService.CreateDepartmentAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mã đơn vị 'ROOT' đã tồn tại*");
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateDepartmentAsync_ValidRequest_UpdatesSuccessfully()
    {
        // Arrange
        var request = new UpdateDepartmentRequest
        {
            ParentDepartmentId = LeafDeptId,
            DepartmentCode = "CHILD_UPDATED",
            DepartmentName = "Phòng Quản lý Hạ tầng (Mới)",
            Email = "new.email@test.com",
            IsActive = false
        };

        // Act
        var result = await _departmentService.UpdateDepartmentAsync(ChildDeptId, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentDepartmentId.Should().Be(LeafDeptId);
        result.DepartmentCode.Should().Be("CHILD_UPDATED");
        result.DepartmentName.Should().Be("Phòng Quản lý Hạ tầng (Mới)");
        result.Email.Should().Be("new.email@test.com");
        result.IsActive.Should().BeFalse();

        var inDb = await _context.Departments.FindAsync(ChildDeptId);
        inDb!.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateDepartmentAsync_SameCode_UpdatesSuccessfully()
    {
        // Arrange
        var request = new UpdateDepartmentRequest
        {
            DepartmentCode = "CHILD", // Same code
            DepartmentName = "Tên mới",
        };

        // Act
        var result = await _departmentService.UpdateDepartmentAsync(ChildDeptId, request);

        // Assert
        result.Should().NotBeNull();
        result.DepartmentName.Should().Be("Tên mới");
    }

    [Fact]
    public async Task UpdateDepartmentAsync_NonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateDepartmentRequest { DepartmentCode = "FAIL", DepartmentName = "Fail" };

        // Act
        Func<Task> act = async () => await _departmentService.UpdateDepartmentAsync(NonExistentDeptId, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateDepartmentAsync_SetSelfAsParent_ThrowsArgumentException()
    {
        // Arrange
        var request = new UpdateDepartmentRequest { ParentDepartmentId = ChildDeptId, DepartmentCode = "CHILD", DepartmentName = "Fail" };

        // Act
        Func<Task> act = async () => await _departmentService.UpdateDepartmentAsync(ChildDeptId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*không thể là đơn vị cha của chính nó*");
    }

    [Fact]
    public async Task UpdateDepartmentAsync_SetChildAsParent_ThrowsArgumentException()
    {
        // Arrange
        var request = new UpdateDepartmentRequest { ParentDepartmentId = ChildDeptId, DepartmentCode = "ROOT", DepartmentName = "Fail" };

        // Act
        Func<Task> act = async () => await _departmentService.UpdateDepartmentAsync(RootDeptId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*tạo thành chu trình*");
    }

    [Fact]
    public async Task UpdateDepartmentAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new UpdateDepartmentRequest { DepartmentCode = "ROOT", DepartmentName = "Fail" };

        // Act
        Func<Task> act = async () => await _departmentService.UpdateDepartmentAsync(ChildDeptId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Mã đơn vị 'ROOT' đã tồn tại*");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteDepartmentAsync_UnlinkedDepartment_DeletesSuccessfully()
    {
        // Arrange
        var initialCount = await _context.Departments.CountAsync();

        // Act
        var result = await _departmentService.DeleteDepartmentAsync(LeafDeptId);

        // Assert
        result.Should().BeTrue();
        (await _context.Departments.CountAsync()).Should().Be(initialCount - 1);
        (await _context.Departments.FindAsync(LeafDeptId)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteDepartmentAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange & Act
        var result = await _departmentService.DeleteDepartmentAsync(NonExistentDeptId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDepartmentAsync_WithChildDepartments_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _departmentService.DeleteDepartmentAsync(RootDeptId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không thể xóa đơn vị đang có đơn vị con*");
    }

    [Fact]
    public async Task DeleteDepartmentAsync_WithActiveMembers_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _departmentService.DeleteDepartmentAsync(ChildDeptId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không thể xóa đơn vị đang có cán bộ hoạt động*");
    }

    #endregion
}