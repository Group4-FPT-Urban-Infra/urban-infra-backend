using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

/// <summary>
/// Facade giữ contract UI ổn định trong khi schema chuyển sang Report 1:N Issues.
/// Summary hiện dùng Issue đầu tiên làm đại diện; detail/timeline được truy theo ReportId.
/// </summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly IIssueService _issues;

    public ReportService(AppDbContext db, IIssueService issues)
    {
        _db = db;
        _issues = issues;
    }

    public Task<ApiResponse<IssueDetailResponse>> CreateAsync(CreateIssueFormRequest request, string reporterId, CancellationToken cancellationToken = default)
        => _issues.CreateIssueAsync(request, reporterId, cancellationToken);

    public Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchAsync(SearchIssuesRequest request, string? currentUserId, CancellationToken cancellationToken = default)
        => _issues.SearchIssuesAsync(request, currentUserId, cancellationToken);

    public Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMineAsync(GetMyIssuesRequest request, string reporterId, CancellationToken cancellationToken = default)
        => _issues.GetMyIssuesAsync(request, reporterId, cancellationToken);

    public async Task<ApiResponse<IssueDetailResponse>> GetByIdAsync(long reportId, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var issueId = await GetPrimaryIssueIdAsync(reportId, cancellationToken);
        return issueId.HasValue
            ? await _issues.GetIssueByIdAsync(issueId.Value, currentUserId, cancellationToken)
            : new ApiResponse<IssueDetailResponse> { Success = false, Message = $"Không tìm thấy Report ID = {reportId}." };
    }

    public Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyAsync(FindNearbyIssuesRequest request, string? currentUserId, CancellationToken cancellationToken = default)
        => _issues.FindNearbyIssuesAsync(request, currentUserId, cancellationToken);

    public async Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetTimelineAsync(long reportId, CancellationToken cancellationToken = default)
    {
        var issueIds = await _db.Issues.Where(x => x.ReportId == reportId).Select(x => x.IssueId).ToListAsync(cancellationToken);
        if (issueIds.Count == 0)
            return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>> { Success = false, Message = $"Không tìm thấy Report ID = {reportId}." };

        var items = new List<IssueTimelineItemResponse>();
        foreach (var issueId in issueIds)
        {
            var result = await _issues.GetIssueTimelineAsync(issueId, cancellationToken);
            if (result.Data is not null) items.AddRange(result.Data);
        }
        return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>
        {
            Success = true,
            Data = items.OrderBy(x => x.CreatedAt).ToList()
        };
    }

    public Task<long?> GetPrimaryIssueIdAsync(long reportId, CancellationToken cancellationToken = default)
        => _db.Issues.Where(x => x.ReportId == reportId).OrderBy(x => x.IssueId)
            .Select(x => (long?)x.IssueId).FirstOrDefaultAsync(cancellationToken);
}
