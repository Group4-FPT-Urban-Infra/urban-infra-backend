using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _context;

    public DepartmentService(AppDbContext context) => _context = context;

    public async Task<List<DepartmentResponse>> GetDepartmentsAsync(
        DepartmentFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.Include(x => x.ParentDepartment).AsNoTracking();

        if (filter.ParentDepartmentId.HasValue)
            query = query.Where(x => x.ParentDepartmentId == filter.ParentDepartmentId);
        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.DepartmentCode.Contains(search) || x.DepartmentName.Contains(search));
        }

        var departments = await query.OrderBy(x => x.DepartmentCode).ToListAsync(cancellationToken);
        return departments.Select(Map).ToList();
    }

    public async Task<DepartmentResponse?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .Include(x => x.ParentDepartment)
            .Include(x => x.ChildDepartments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == id, cancellationToken);

        if (department is null) return null;
        var response = Map(department);
        response.ChildDepartments = department.ChildDepartments.OrderBy(x => x.DepartmentCode).Select(Map).ToList();
        return response;
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateParentAsync(request.ParentDepartmentId, null, cancellationToken);
        var code = NormalizeCode(request.DepartmentCode);
        await EnsureCodeUniqueAsync(code, null, cancellationToken);

        var department = new Department
        {
            ParentDepartmentId = request.ParentDepartmentId,
            DepartmentCode = code,
            DepartmentName = request.DepartmentName.Trim(),
            Email = Clean(request.Email),
            Phone = Clean(request.Phone),
            Address = Clean(request.Address),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetDepartmentByIdAsync(department.DepartmentId, cancellationToken) ?? Map(department);
    }

    public async Task<DepartmentResponse> UpdateDepartmentAsync(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(x => x.DepartmentId == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn vị có ID = {id}.");

        await ValidateParentAsync(request.ParentDepartmentId, id, cancellationToken);
        var code = NormalizeCode(request.DepartmentCode);
        await EnsureCodeUniqueAsync(code, id, cancellationToken);

        department.ParentDepartmentId = request.ParentDepartmentId;
        department.DepartmentCode = code;
        department.DepartmentName = request.DepartmentName.Trim();
        department.Email = Clean(request.Email);
        department.Phone = Clean(request.Phone);
        department.Address = Clean(request.Address);
        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetDepartmentByIdAsync(id, cancellationToken) ?? Map(department);
    }

    public async Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(x => x.DepartmentId == id, cancellationToken);
        if (department is null) return false;

        if (await _context.Departments.AnyAsync(x => x.ParentDepartmentId == id, cancellationToken))
            throw new InvalidOperationException("Không thể xóa đơn vị đang có đơn vị con.");
        if (await _context.DepartmentMembers.AnyAsync(x => x.DepartmentId == id && x.IsActive, cancellationToken))
            throw new InvalidOperationException("Không thể xóa đơn vị đang có cán bộ hoạt động.");

        var membershipHistory = await _context.DepartmentMembers
            .Where(x => x.DepartmentId == id)
            .ToListAsync(cancellationToken);
        _context.DepartmentMembers.RemoveRange(membershipHistory);
        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateParentAsync(int? parentId, int? currentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue) return;
        if (parentId == currentId) throw new ArgumentException("Đơn vị không thể là đơn vị cha của chính nó.");
        if (!await _context.Departments.AnyAsync(x => x.DepartmentId == parentId, cancellationToken))
            throw new ArgumentException($"Đơn vị cha có ID = {parentId} không tồn tại.");

        var ancestorId = parentId;
        var visited = new HashSet<int>();
        while (ancestorId.HasValue && visited.Add(ancestorId.Value))
        {
            if (ancestorId == currentId)
                throw new ArgumentException("Quan hệ đơn vị cha tạo thành chu trình.");
            ancestorId = await _context.Departments
                .Where(x => x.DepartmentId == ancestorId)
                .Select(x => x.ParentDepartmentId)
                .SingleAsync(cancellationToken);
        }
    }

    private async Task EnsureCodeUniqueAsync(string code, int? exceptId, CancellationToken cancellationToken)
    {
        if (await _context.Departments.AnyAsync(
                x => x.DepartmentCode == code && (!exceptId.HasValue || x.DepartmentId != exceptId), cancellationToken))
            throw new InvalidOperationException($"Mã đơn vị '{code}' đã tồn tại.");
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DepartmentResponse Map(Department x) => new()
    {
        DepartmentId = x.DepartmentId,
        ParentDepartmentId = x.ParentDepartmentId,
        ParentDepartmentName = x.ParentDepartment?.DepartmentName,
        DepartmentCode = x.DepartmentCode,
        DepartmentName = x.DepartmentName,
        Email = x.Email,
        Phone = x.Phone,
        Address = x.Address,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
