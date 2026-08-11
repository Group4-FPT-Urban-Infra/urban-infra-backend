using UrbanInfraSystem.Application.DTOs.IssueStatuses;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssueStatusService
{
    Task<IReadOnlyList<IssueStatusResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssueStatusResponse>> GetActiveAsync(CancellationToken ct = default);
    Task<IssueStatusResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IssueStatusResponse> CreateAsync(CreateIssueStatusRequest request, CancellationToken ct = default);
    Task<IssueStatusResponse?> UpdateAsync(int id, UpdateIssueStatusRequest request, CancellationToken ct = default);
    Task<bool> DeactivateAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
