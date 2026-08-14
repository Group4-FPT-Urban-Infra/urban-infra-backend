using UrbanInfraSystem.Application.DTOs.DepartmentManager;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IDepartmentManagerDashboardService
{
    Task<DepartmentManagerDashboardStatsResponse> GetStatsAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<TeamWorkloadResponse> GetTeamWorkloadAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentManagerIssueSummary>> GetUnassignedIssuesAsync(int departmentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
