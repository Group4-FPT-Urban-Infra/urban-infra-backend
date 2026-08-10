using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.IssueStatuses;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssueStatusService : IIssueStatusService
{
    private readonly AppDbContext _context;

    public IssueStatusService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<IssueStatusResponse>> GetAllAsync(CancellationToken ct = default)
        => await _context.IssueStatuses.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(ToResponse()).ToListAsync(ct);

    public async Task<IReadOnlyList<IssueStatusResponse>> GetActiveAsync(CancellationToken ct = default)
        => await _context.IssueStatuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
            .Select(ToResponse()).ToListAsync(ct);

    public async Task<IssueStatusResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.IssueStatuses.AsNoTracking().Where(x => x.StatusId == id)
            .Select(ToResponse()).SingleOrDefaultAsync(ct);

    public async Task<IssueStatusResponse> CreateAsync(CreateIssueStatusRequest request, CancellationToken ct = default)
    {
        var code = NormalizeCode(request.StatusCode);
        await EnsureUniqueAsync(code, request.DisplayOrder, null, ct);
        var entity = new IssueStatus
        {
            StatusCode = code,
            StatusName = request.StatusName.Trim(),
            IsClosed = request.IsClosed,
            IsPublicVisible = request.IsPublicVisible,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };
        _context.IssueStatuses.Add(entity);
        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IssueStatusResponse?> UpdateAsync(int id, UpdateIssueStatusRequest request, CancellationToken ct = default)
    {
        var entity = await _context.IssueStatuses.SingleOrDefaultAsync(x => x.StatusId == id, ct);
        if (entity is null) return null;

        var code = request.StatusCode is null ? entity.StatusCode : NormalizeCode(request.StatusCode);
        var order = request.DisplayOrder ?? entity.DisplayOrder;
        await EnsureUniqueAsync(code, order, id, ct);

        entity.StatusCode = code;
        if (request.StatusName is not null) entity.StatusName = request.StatusName.Trim();
        if (request.IsClosed.HasValue) entity.IsClosed = request.IsClosed.Value;
        if (request.IsPublicVisible.HasValue) entity.IsPublicVisible = request.IsPublicVisible.Value;
        entity.DisplayOrder = order;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.IssueStatuses.SingleOrDefaultAsync(x => x.StatusId == id, ct);
        if (entity is null) return false;
        entity.IsActive = false;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.IssueStatuses.SingleOrDefaultAsync(x => x.StatusId == id, ct);
        if (entity is null) return false;
        _context.IssueStatuses.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task EnsureUniqueAsync(string code, short displayOrder, int? excludingId, CancellationToken ct)
    {
        if (await _context.IssueStatuses.AnyAsync(x => x.StatusCode == code && x.StatusId != excludingId, ct))
            throw new InvalidOperationException($"Mã trạng thái '{code}' đã tồn tại.");
        if (await _context.IssueStatuses.AnyAsync(x => x.DisplayOrder == displayOrder && x.StatusId != excludingId, ct))
            throw new InvalidOperationException($"Thứ tự hiển thị {displayOrder} đã tồn tại.");
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static IssueStatusResponse Map(IssueStatus x) => new()
    {
        StatusId = x.StatusId, StatusCode = x.StatusCode, StatusName = x.StatusName,
        IsClosed = x.IsClosed, IsPublicVisible = x.IsPublicVisible,
        DisplayOrder = x.DisplayOrder, IsActive = x.IsActive
    };
    private static System.Linq.Expressions.Expression<Func<IssueStatus, IssueStatusResponse>> ToResponse()
        => x => new IssueStatusResponse
        {
            StatusId = x.StatusId, StatusCode = x.StatusCode, StatusName = x.StatusName,
            IsClosed = x.IsClosed, IsPublicVisible = x.IsPublicVisible,
            DisplayOrder = x.DisplayOrder, IsActive = x.IsActive
        };
}
