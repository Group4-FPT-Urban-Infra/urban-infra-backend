using UrbanInfraSystem.Application.DTOs.IssuePriorities;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssuePriorityService
{
    Task<IReadOnlyList<IssuePriorityResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssuePriorityResponse>> GetActiveAsync(CancellationToken ct = default);
    Task<IssuePriorityResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IssuePriorityResponse> CreateAsync(CreateIssuePriorityRequest request, CancellationToken ct = default);
    Task<IssuePriorityResponse?> UpdateAsync(int id, UpdateIssuePriorityRequest request, CancellationToken ct = default);
    Task<bool> DeactivateAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
