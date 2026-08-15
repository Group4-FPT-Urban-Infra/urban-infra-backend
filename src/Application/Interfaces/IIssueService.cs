using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UrbanInfraSystem.Application.DTOs.Dashboard;
using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.Interfaces;

public interface IIssueService
{
    Task<ApiResponse<DashboardStatsResponse>> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Trả về 6 chỉ số KPI dành cho Admin Dashboard kèm trend % theo tuần.</summary>
    Task<ApiResponse<AdminKpiResponse>> GetAdminKpiAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về danh sách điểm dữ liệu cho biểu đồ xu hướng sự cố.
    /// period: "ThisWeek" → 7 ngày (Mon–Sun) | "ThisMonth" → từng ngày trong tháng | "ThisYear" → 12 tháng.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<TrendDataPoint>>> GetIncidentTrendsAsync(string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về phân bổ sự cố theo danh mục (IssueType).
    /// </summary>
    Task<ApiResponse<IReadOnlyList<CategoryDistributionPoint>>> GetCategoryDistributionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về dữ liệu heatmap các cụm sự cố theo khu vực trong khoảng thời gian (24h, 7d, 30d).
    /// </summary>
    Task<ApiResponse<IReadOnlyList<HeatmapDataPoint>>> GetIncidentHeatmapAsync(string timeframe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về danh sách Audit Logs / Hoạt động hệ thống mới nhất.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<AuditLogResponse>>> GetRecentAuditLogsAsync(int limit = 10, CancellationToken cancellationToken = default);

    Task<ApiResponse<IssueDetailResponse>> CreateIssueAsync(CreateIssueFormRequest request, string reporterId, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchIssuesAsync(SearchIssuesRequest request, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMyIssuesAsync(GetMyIssuesRequest request, string reporterId, CancellationToken cancellationToken = default);

    Task<ApiResponse<IssueDetailResponse>> GetIssueByIdAsync(long issueId, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyIssuesAsync(FindNearbyIssuesRequest request, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetIssueTimelineAsync(long issueId, CancellationToken cancellationToken = default);
}
