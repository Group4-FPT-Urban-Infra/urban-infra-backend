using UrbanInfraSystem.Application.DTOs.DepartmentManager;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IDepartmentManagerIssueService
{
    Task<IReadOnlyList<DepartmentManagerIssueSummary>> GetIssuesAsync(int departmentId, DepartmentManagerIssueListRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentManagerIssueDetailResponse?> GetIssueDetailAsync(long issueId, CancellationToken cancellationToken = default);
    Task<DepartmentManagerIssueDetailResponse> AssignIssueAsync(long issueId, AssignIssueRequest request, string assignedBy, CancellationToken cancellationToken = default);
    Task<DepartmentManagerIssueDetailResponse> UpdateIssueStatusAsync(long issueId, DepartmentManagerUpdateIssueStatusRequest request, string userId, CancellationToken cancellationToken = default);
}
