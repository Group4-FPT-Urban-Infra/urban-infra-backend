using UrbanInfraSystem.Application.DTOs.DepartmentManager;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IDepartmentManagerSlaService
{
    Task<SlaOverviewResponse> GetOverviewAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SlaNearDeadlineItem>> GetNearDeadlineAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<SlaHistoryResponse> GetHistoryAsync(int departmentId, int months = 6, CancellationToken cancellationToken = default);
}
