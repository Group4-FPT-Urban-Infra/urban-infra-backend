using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.SlaPolicies;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class SlaPolicyService : ISlaPolicyService
{
    private readonly AppDbContext _context;

    public SlaPolicyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SlaPolicyResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.SlaPolicies
            .Include(s => s.IssueType)
            .Include(s => s.IssuePriority)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task<SlaPolicyResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.SlaPolicies
            .Include(s => s.IssueType)
            .Include(s => s.IssuePriority)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return item == null ? null : Map(item);
    }

    public async Task<SlaPolicyResponse> CreateAsync(CreateSlaPolicyRequest request, CancellationToken ct = default)
    {
        // Validate existence of IssueType
        var issueType = await _context.IssueTypes.FirstOrDefaultAsync(it => it.IssueTypeId == request.IssueTypeId, ct);
        if (issueType == null || !issueType.IsActive)
            throw new ArgumentException("IssueType does not exist or is inactive.");

        var priority = await _context.IssuePriorities.FirstOrDefaultAsync(p => p.PriorityId == request.PriorityId, ct);
        if (priority == null || !priority.IsActive)
            throw new ArgumentException("Priority does not exist or is inactive.");

        if (request.FirstResponseMinutes > request.ResolutionMinutes)
            throw new ArgumentException("FirstResponseMinutes must be less than or equal to ResolutionMinutes.");

        var exists = await _context.SlaPolicies.AnyAsync(s => s.IssueTypeId == request.IssueTypeId && s.PriorityId == request.PriorityId, ct);
        if (exists)
            throw new InvalidOperationException("SLA policy for the given IssueType and Priority already exists.");

        var entity = new SlaPolicy
        {
            IssueTypeId = request.IssueTypeId,
            PriorityId = request.PriorityId,
            ResolutionMinutes = request.ResolutionMinutes,
            FirstResponseMinutes = request.FirstResponseMinutes,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.SlaPolicies.Add(entity);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct) ?? Map(entity);
    }

    public async Task<SlaPolicyResponse?> UpdateAsync(Guid id, UpdateSlaPolicyRequest request, CancellationToken ct = default)
    {
        var entity = await _context.SlaPolicies.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity == null) return null;

        var issueType = await _context.IssueTypes.FirstOrDefaultAsync(it => it.IssueTypeId == request.IssueTypeId, ct);
        if (issueType == null || !issueType.IsActive)
            throw new ArgumentException("IssueType does not exist or is inactive.");

        var priority = await _context.IssuePriorities.FirstOrDefaultAsync(p => p.PriorityId == request.PriorityId, ct);
        if (priority == null || !priority.IsActive)
            throw new ArgumentException("Priority does not exist or is inactive.");

        if (request.FirstResponseMinutes > request.ResolutionMinutes)
            throw new ArgumentException("FirstResponseMinutes must be less than or equal to ResolutionMinutes.");

        var duplicate = await _context.SlaPolicies.AnyAsync(s => s.Id != id && s.IssueTypeId == request.IssueTypeId && s.PriorityId == request.PriorityId, ct);
        if (duplicate)
            throw new InvalidOperationException("SLA policy for the given IssueType and Priority already exists.");

        entity.IssueTypeId = request.IssueTypeId;
        entity.PriorityId = request.PriorityId;
        entity.ResolutionMinutes = request.ResolutionMinutes;
        entity.FirstResponseMinutes = request.FirstResponseMinutes;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.SlaPolicies.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static SlaPolicyResponse Map(SlaPolicy s) => new SlaPolicyResponse
    {
        Id = s.Id,
        IssueTypeId = s.IssueTypeId,
        IssueTypeName = s.IssueType?.TypeName?? string.Empty,
        PriorityId = s.PriorityId,
        PriorityName = s.IssuePriority?.PriorityName ?? string.Empty,
        ResolutionMinutes = s.ResolutionMinutes,
        FirstResponseMinutes = s.FirstResponseMinutes,
        CreatedAtUtc = s.CreatedAtUtc,
        UpdatedAtUtc = s.UpdatedAtUtc
    };
}
