using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Departments;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class DepartmentMemberService : IDepartmentMemberService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DepartmentMemberService(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<DepartmentMemberResponse>> GetMembersAsync(
        int departmentId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await EnsureDepartmentExistsAsync(departmentId, false, cancellationToken);
        var query = from member in _context.DepartmentMembers.AsNoTracking()
                    join user in _context.Users.AsNoTracking() on member.UserId equals user.Id
                    where member.DepartmentId == departmentId
                    select new { member, user };
        if (activeOnly) query = query.Where(x => x.member.IsActive);

        return await query.OrderBy(x => x.user.FullName).Select(x => new DepartmentMemberResponse
        {
            DepartmentId = x.member.DepartmentId,
            UserId = x.member.UserId,
            FullName = x.user.FullName,
            Email = x.user.Email,
            JobTitle = x.member.JobTitle,
            IsManager = x.member.IsManager,
            JoinedAt = x.member.JoinedAt,
            LeftAt = x.member.LeftAt,
            IsActive = x.member.IsActive
        }).ToListAsync(cancellationToken);
    }

    public async Task<DepartmentMemberResponse> AssignMemberAsync(
        int departmentId,
        AssignDepartmentMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureDepartmentExistsAsync(departmentId, true, cancellationToken);
        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new ArgumentException($"Người dùng có ID = {request.UserId} không tồn tại.");
        if (!user.IsActive) throw new InvalidOperationException("Không thể gán người dùng đã bị khóa.");
        if (!await _userManager.IsInRoleAsync(user, Roles.DepartmentStaff))
            throw new InvalidOperationException("Người dùng phải có vai trò DepartmentStaff trước khi được gán vào đơn vị.");

        var member = await _context.DepartmentMembers.FindAsync([departmentId, request.UserId], cancellationToken);
        // If already active, we just update their role instead of throwing an exception.

        if (member is null)
        {
            member = new DepartmentMember { DepartmentId = departmentId, UserId = request.UserId };
            _context.DepartmentMembers.Add(member);
        }

        member.JobTitle = Clean(request.JobTitle);
        member.IsManager = request.IsManager;
        member.JoinedAt = DateTime.UtcNow;
        member.LeftAt = null;
        member.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);

        return new DepartmentMemberResponse
        {
            DepartmentId = departmentId,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            JobTitle = member.JobTitle,
            IsManager = member.IsManager,
            JoinedAt = member.JoinedAt,
            IsActive = true
        };
    }

    public async Task<bool> RemoveMemberAsync(int departmentId, string userId, CancellationToken cancellationToken = default)
    {
        await EnsureDepartmentExistsAsync(departmentId, false, cancellationToken);
        var member = await _context.DepartmentMembers.FindAsync([departmentId, userId], cancellationToken);
        if (member is null || !member.IsActive) return false;

        member.IsActive = false;
        member.IsManager = false;
        member.LeftAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureDepartmentExistsAsync(int id, bool requireActive, CancellationToken cancellationToken)
    {
        var department = await _context.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentId == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy đơn vị có ID = {id}.");
        if (requireActive && !department.IsActive)
            throw new InvalidOperationException("Không thể gán cán bộ vào đơn vị đã ngừng hoạt động.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
