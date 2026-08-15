using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>Facade chuyển tiếp contract Report trong giai đoạn tách Report khỏi Issue.</summary>
public interface IReportService
{
    Task<ApiResponse<IssueDetailResponse>> CreateAsync(CreateIssueFormRequest request, string reporterId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchAsync(SearchIssuesRequest request, string? currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMineAsync(GetMyIssuesRequest request, string reporterId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IssueDetailResponse>> GetByIdAsync(long reportId, string? currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyAsync(FindNearbyIssuesRequest request, string? currentUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetTimelineAsync(long reportId, CancellationToken cancellationToken = default);
    Task<long?> GetPrimaryIssueIdAsync(long reportId, CancellationToken cancellationToken = default);
}
