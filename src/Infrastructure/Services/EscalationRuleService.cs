using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.EscalationRules;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Identity;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class EscalationRuleService : IEscalationRuleService
{
    private readonly AppDbContext _db;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public EscalationRuleService(AppDbContext db, RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<EscalationRuleResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _db.EscalationRules
            .Include(x => x.TargetDepartment)
            .Include(x => x.SlaPolicy)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task<EscalationRuleResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.EscalationRules
            .Include(x => x.TargetDepartment)
            .Include(x => x.SlaPolicy)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return item == null ? null : Map(item);
    }

    public async Task<EscalationRuleResponse> CreateAsync(CreateEscalationRuleRequest request, CancellationToken ct = default)
    {
        // Validate SlaPolicy exists
        var slaExists = await _db.SlaPolicies.AnyAsync(s => s.Id == request.SlaPolicyId, ct);
        if (!slaExists) throw new ArgumentException("SlaPolicy not found.");

        if (request.OverdueMinutes <= 0) throw new ArgumentException("OverdueMinutes must be > 0.");

        if (request.EscalationLevel <= 0) throw new ArgumentException("EscalationLevel must be >= 1.");

        if (request.TargetDepartmentId == null && string.IsNullOrWhiteSpace(request.TargetRoleName))
            throw new ArgumentException("At least one target (department or role) must be specified.");

        if (request.TargetDepartmentId.HasValue)
        {
            var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentId == request.TargetDepartmentId.Value, ct);
            if (!deptExists) throw new ArgumentException("TargetDepartment not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.TargetRoleName))
        {
            var roleExists = await _roleManager.RoleExistsAsync(request.TargetRoleName.Trim());
            if (!roleExists) throw new ArgumentException("TargetRole not found.");
        }

        // Tách xử lý chuỗi ra biến cục bộ trước khi truyền vào Expression Lambda để tránh lỗi CS8072
        var targetRoleName = string.IsNullOrWhiteSpace(request.TargetRoleName) ? null : request.TargetRoleName.Trim();

        // Check duplicate exact combination (include escalation level)
        var duplicate = await _db.EscalationRules.AnyAsync(x =>
            x.SlaPolicyId == request.SlaPolicyId
            && x.EscalationLevel == request.EscalationLevel
            && x.OverdueMinutes == request.OverdueMinutes
            && x.TargetDepartmentId == request.TargetDepartmentId
            && x.TargetRoleName == targetRoleName, ct);

        if (duplicate) throw new InvalidOperationException("Escalation rule with the same SLA, overdue minutes and target already exists.");

        var entity = new EscalationRule
        {
            SlaPolicyId = request.SlaPolicyId,
            OverdueMinutes = request.OverdueMinutes,
            EscalationLevel = request.EscalationLevel,
            TargetDepartmentId = request.TargetDepartmentId,
            TargetRoleName = targetRoleName,
            NotificationTitle = string.IsNullOrWhiteSpace(request.NotificationTitle) ? null : request.NotificationTitle.Trim(),
            NotificationTemplate = string.IsNullOrWhiteSpace(request.NotificationTemplate) ? null : request.NotificationTemplate.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.EscalationRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct) ?? Map(entity);
    }

    public async Task<EscalationRuleResponse?> UpdateAsync(Guid id, UpdateEscalationRuleRequest request, CancellationToken ct = default)
    {
        var entity = await _db.EscalationRules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return null;

        var slaExists = await _db.SlaPolicies.AnyAsync(s => s.Id == request.SlaPolicyId, ct);
        if (!slaExists) throw new ArgumentException("SlaPolicy not found.");

        if (request.OverdueMinutes <= 0) throw new ArgumentException("OverdueMinutes must be > 0.");

        if (request.EscalationLevel <= 0) throw new ArgumentException("EscalationLevel must be >= 1.");

        if (request.TargetDepartmentId == null && string.IsNullOrWhiteSpace(request.TargetRoleName))
            throw new ArgumentException("At least one target (department or role) must be specified.");

        if (request.TargetDepartmentId.HasValue)
        {
            var deptExists = await _db.Departments.AnyAsync(d => d.DepartmentId == request.TargetDepartmentId.Value, ct);
            if (!deptExists) throw new ArgumentException("TargetDepartment not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.TargetRoleName))
        {
            var roleExists = await _roleManager.RoleExistsAsync(request.TargetRoleName.Trim());
            if (!roleExists) throw new ArgumentException("TargetRole not found.");
        }

        // Tách xử lý chuỗi ra biến cục bộ trước khi truyền vào Expression Lambda để tránh lỗi CS8072
        var targetRoleName = string.IsNullOrWhiteSpace(request.TargetRoleName) ? null : request.TargetRoleName.Trim();

        var duplicate = await _db.EscalationRules.AnyAsync(x =>
            x.Id != id
            && x.SlaPolicyId == request.SlaPolicyId
            && x.EscalationLevel == request.EscalationLevel
            && x.OverdueMinutes == request.OverdueMinutes
            && x.TargetDepartmentId == request.TargetDepartmentId
            && x.TargetRoleName == targetRoleName, ct);

        if (duplicate) throw new InvalidOperationException("Escalation rule with the same SLA, overdue minutes and target already exists.");

        entity.SlaPolicyId = request.SlaPolicyId;
        entity.OverdueMinutes = request.OverdueMinutes;
        entity.EscalationLevel = request.EscalationLevel;
        entity.TargetDepartmentId = request.TargetDepartmentId;
        entity.TargetRoleName = targetRoleName;
        entity.NotificationTitle = string.IsNullOrWhiteSpace(request.NotificationTitle) ? null : request.NotificationTitle.Trim();
        entity.NotificationTemplate = string.IsNullOrWhiteSpace(request.NotificationTemplate) ? null : request.NotificationTemplate.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.EscalationRules.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static EscalationRuleResponse Map(EscalationRule x) => new()
    {
        Id = x.Id,
        SlaPolicyId = x.SlaPolicyId,
        OverdueMinutes = x.OverdueMinutes,
        TargetDepartmentId = x.TargetDepartmentId,
        TargetDepartmentName = x.TargetDepartment?.DepartmentName,
        TargetRoleName = x.TargetRoleName,
        EscalationLevel = x.EscalationLevel,
        NotificationTitle = x.NotificationTitle,
        NotificationTemplate = x.NotificationTemplate,
        IsActive = x.IsActive,
        CreatedAtUtc = x.CreatedAtUtc,
        UpdatedAtUtc = x.UpdatedAtUtc
    };
}