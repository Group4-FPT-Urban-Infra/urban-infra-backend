using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.IssuePriorities;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssuePriorityService : IIssuePriorityService
{
    private readonly AppDbContext _context;

    public IssuePriorityService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<IssuePriorityResponse>> GetAllAsync(CancellationToken ct = default)
        => await _context.IssuePriorities.AsNoTracking()
            .OrderBy(x => x.SeverityRank).Select(ToResponse()).ToListAsync(ct);

    public async Task<IReadOnlyList<IssuePriorityResponse>> GetActiveAsync(CancellationToken ct = default)
        => await _context.IssuePriorities.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SeverityRank).Select(ToResponse()).ToListAsync(ct);

    public async Task<IssuePriorityResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.IssuePriorities.AsNoTracking().Where(x => x.PriorityId == id)
            .Select(ToResponse()).SingleOrDefaultAsync(ct);

    public async Task<IssuePriorityResponse> CreateAsync(CreateIssuePriorityRequest request, CancellationToken ct = default)
    {
        var code = NormalizeCode(request.PriorityCode);
        await EnsureUniqueAsync(code, request.SeverityRank, null, ct);

        var entity = new IssuePriority
        {
            PriorityCode = code,
            PriorityName = request.PriorityName.Trim(),
            SeverityRank = request.SeverityRank,
            IsActive = request.IsActive
        };
        _context.IssuePriorities.Add(entity);
        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IssuePriorityResponse?> UpdateAsync(int id, UpdateIssuePriorityRequest request, CancellationToken ct = default)
    {
        var entity = await _context.IssuePriorities.SingleOrDefaultAsync(x => x.PriorityId == id, ct);
        if (entity is null) return null;

        var code = request.PriorityCode is null ? entity.PriorityCode : NormalizeCode(request.PriorityCode);
        var rank = request.SeverityRank ?? entity.SeverityRank;
        await EnsureUniqueAsync(code, rank, id, ct);

        entity.PriorityCode = code;
        if (request.PriorityName is not null) entity.PriorityName = request.PriorityName.Trim();
        entity.SeverityRank = rank;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.IssuePriorities.SingleOrDefaultAsync(x => x.PriorityId == id, ct);
        if (entity is null) return false;
        entity.IsActive = false;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.IssuePriorities.SingleOrDefaultAsync(x => x.PriorityId == id, ct);
        if (entity is null) return false;
        _context.IssuePriorities.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task EnsureUniqueAsync(string code, byte rank, int? excludingId, CancellationToken ct)
    {
        if (await _context.IssuePriorities.AnyAsync(x => x.PriorityCode == code && x.PriorityId != excludingId, ct))
            throw new InvalidOperationException($"Mã mức ưu tiên '{code}' đã tồn tại.");
        if (await _context.IssuePriorities.AnyAsync(x => x.SeverityRank == rank && x.PriorityId != excludingId, ct))
            throw new InvalidOperationException($"Thứ hạng mức độ {rank} đã tồn tại.");
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static IssuePriorityResponse Map(IssuePriority x) => new()
    {
        PriorityId = x.PriorityId, PriorityCode = x.PriorityCode, PriorityName = x.PriorityName,
        SeverityRank = x.SeverityRank, IsActive = x.IsActive
    };
    private static System.Linq.Expressions.Expression<Func<IssuePriority, IssuePriorityResponse>> ToResponse()
        => x => new IssuePriorityResponse
        {
            PriorityId = x.PriorityId, PriorityCode = x.PriorityCode, PriorityName = x.PriorityName,
            SeverityRank = x.SeverityRank, IsActive = x.IsActive
        };
}
