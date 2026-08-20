using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Reports;

namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>Facade chuyển tiếp contract Report trong giai đoạn tách Report khỏi Issue.</summary>
public interface IReportService
{
    Task<ApiResponse<ReportDetailResponse>> CreateAsync(CreateIssueFormRequest request, string reporterId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchAsync(SearchIssuesRequest request, string? currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<ReportSummaryResponse>>> GetMineAsync(GetMyIssuesRequest request, string reporterId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ReportDetailResponse>> GetByIdAsync(long reportId, string? currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<ReportUpdateResponse>>> GetMyRecentUpdatesAsync(string reporterId, int limit, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyAsync(FindNearbyIssuesRequest request, string? currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetTimelineAsync(long reportId, CancellationToken cancellationToken = default);
}
