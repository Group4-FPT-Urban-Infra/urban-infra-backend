using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.Dashboard;
using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssueService
{
    Task<ApiResponse<DashboardStatsResponse>> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<ApiResponse<IssueDetailResponse>> CreateIssueAsync(CreateIssueFormRequest request, string reporterId, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchIssuesAsync(SearchIssuesRequest request, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMyIssuesAsync(GetMyIssuesRequest request, string reporterId, CancellationToken cancellationToken = default);

    Task<ApiResponse<IssueDetailResponse>> GetIssueByIdAsync(long issueId, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyIssuesAsync(FindNearbyIssuesRequest request, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetIssueTimelineAsync(long issueId, CancellationToken cancellationToken = default);
}
