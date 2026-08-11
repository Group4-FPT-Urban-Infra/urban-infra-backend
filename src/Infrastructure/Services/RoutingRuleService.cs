using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.RoutingRules;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class RoutingRuleService : IRoutingRuleService
{
    private readonly AppDbContext _context;

    public RoutingRuleService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<RoutingRuleResponse>> GetAllAsync(
        RoutingRuleFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = Query();
        if (!filter.IncludeInactive) query = query.Where(x => x.IsActive);
        if (filter.IssueTypeId.HasValue) query = query.Where(x => x.IssueTypeId == filter.IssueTypeId);
        if (filter.AreaId.HasValue) query = query.Where(x => x.AreaId == filter.AreaId);
        if (filter.DepartmentId.HasValue) query = query.Where(x => x.DepartmentId == filter.DepartmentId);

        var rules = await query
            .OrderBy(x => x.Area.AreaCode)
            .ThenBy(x => x.IssueType.TypeCode)
            .ToListAsync(cancellationToken);
        return rules.Select(Map).ToList();
    }

    public async Task<RoutingRuleResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rule = await Query().FirstOrDefaultAsync(x => x.RoutingRuleId == id, cancellationToken);
        return rule is null ? null : Map(rule);
    }

    public async Task<RoutingRuleResponse?> ResolveAsync(
        int issueTypeId,
        int areaId,
        CancellationToken cancellationToken = default)
    {
        var rule = await Query().FirstOrDefaultAsync(
            x => x.IssueTypeId == issueTypeId && x.AreaId == areaId && x.IsActive,
            cancellationToken);
        return rule is null ? null : Map(rule);
    }

    public async Task<RoutingRuleResponse> CreateAsync(
        CreateRoutingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(request.IssueTypeId, request.AreaId, request.DepartmentId, cancellationToken);
        if (await _context.RoutingRules.AnyAsync(
                x => x.IssueTypeId == request.IssueTypeId && x.AreaId == request.AreaId,
                cancellationToken))
            throw new InvalidOperationException("Quy tắc định tuyến cho loại sự cố và khu vực này đã tồn tại.");

        var rule = new RoutingRule
        {
            IssueTypeId = request.IssueTypeId,
            AreaId = request.AreaId,
            DepartmentId = request.DepartmentId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.RoutingRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(rule.RoutingRuleId, cancellationToken) ?? throw new InvalidOperationException("Không thể đọc quy tắc vừa tạo.");
    }

    public async Task<RoutingRuleResponse?> UpdateAsync(
        int id,
        UpdateRoutingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var rule = await _context.RoutingRules.FirstOrDefaultAsync(x => x.RoutingRuleId == id, cancellationToken);
        if (rule is null) return null;

        await ValidateReferencesAsync(request.IssueTypeId, request.AreaId, request.DepartmentId, cancellationToken);
        if (await _context.RoutingRules.AnyAsync(
                x => x.RoutingRuleId != id && x.IssueTypeId == request.IssueTypeId && x.AreaId == request.AreaId,
                cancellationToken))
            throw new InvalidOperationException("Quy tắc định tuyến cho loại sự cố và khu vực này đã tồn tại.");

        rule.IssueTypeId = request.IssueTypeId;
        rule.AreaId = request.AreaId;
        rule.DepartmentId = request.DepartmentId;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rule = await _context.RoutingRules.FirstOrDefaultAsync(x => x.RoutingRuleId == id, cancellationToken);
        if (rule is null) return false;
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(int issueTypeId, int areaId, int departmentId, CancellationToken cancellationToken)
    {
        if (!await _context.IssueTypes.AnyAsync(x => x.IssueTypeId == issueTypeId && x.IsActive, cancellationToken))
            throw new ArgumentException($"Loại sự cố có ID = {issueTypeId} không tồn tại hoặc đã ngừng hoạt động.");
        if (!await _context.Areas.AnyAsync(x => x.AreaId == areaId && x.IsActive, cancellationToken))
            throw new ArgumentException($"Khu vực có ID = {areaId} không tồn tại hoặc đã ngừng hoạt động.");
        if (!await _context.Departments.AnyAsync(x => x.DepartmentId == departmentId && x.IsActive, cancellationToken))
            throw new ArgumentException($"Đơn vị có ID = {departmentId} không tồn tại hoặc đã ngừng hoạt động.");
    }

    private IQueryable<RoutingRule> Query() => _context.RoutingRules
        .Include(x => x.IssueType)
        .Include(x => x.Area)
        .Include(x => x.Department)
        .AsNoTracking();

    private static RoutingRuleResponse Map(RoutingRule x) => new()
    {
        RoutingRuleId = x.RoutingRuleId,
        IssueTypeId = x.IssueTypeId,
        IssueTypeCode = x.IssueType.TypeCode,
        IssueTypeName = x.IssueType.TypeName,
        AreaId = x.AreaId,
        AreaCode = x.Area.AreaCode,
        AreaName = x.Area.AreaName,
        DepartmentId = x.DepartmentId,
        DepartmentCode = x.Department.DepartmentCode,
        DepartmentName = x.Department.DepartmentName,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
