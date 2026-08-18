using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.DTOs.Reports;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly IIssueService _issues;

    public ReportService(AppDbContext db, IIssueService issues)
    {
        _db = db;
        _issues = issues;
    }

    public async Task<ApiResponse<ReportDetailResponse>> CreateAsync(CreateIssueFormRequest request, string reporterId, CancellationToken ct = default)
    {
        var created = await _issues.CreateIssueAsync(request, reporterId, ct);
        if (!created.Success || created.Data is null)
            return new ApiResponse<ReportDetailResponse> { Success = false, Message = created.Message };
        var reportId = await _db.Issues.Where(i => i.IssueId == created.Data.Id).Select(i => i.ReportId).SingleAsync(ct);
        var detail = await GetByIdAsync(reportId, reporterId, ct);
        detail.Message = created.Message;
        return detail;
    }

    public Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchAsync(SearchIssuesRequest request, string? userId, CancellationToken ct = default)
        => _issues.SearchIssuesAsync(request, userId, ct);

    public async Task<ApiResponse<PagedResponse<ReportSummaryResponse>>> GetMineAsync(GetMyIssuesRequest request, string reporterId, CancellationToken ct = default)
    {
        var query = ReportQuery().Where(r => r.ReporterId == reporterId && !r.IsArchived);
        if (request.StatusCodes is { Length: > 0 })
        {
            var codes = request.StatusCodes.Select(x => x.ToUpper()).ToList();
            query = query.Where(r => r.Issues.Any(i => codes.Contains(i.Status.StatusCode.ToUpper())));
        }
        var total = await query.LongCountAsync(ct);
        var reports = await query.OrderByDescending(r => r.ReportedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        var upvoted = await _db.ReportUpvotes.Where(x => x.UserId == reporterId).Select(x => x.ReportId).ToHashSetAsync(ct);
        return new ApiResponse<PagedResponse<ReportSummaryResponse>>
        {
            Success = true,
            Data = new PagedResponse<ReportSummaryResponse>
            {
                Items = reports.Select(r => MapSummary(r, upvoted.Contains(r.ReportId))).ToList(),
                Page = request.Page, PageSize = request.PageSize, TotalItems = total,
                TotalPages = (int)Math.Ceiling((double)total / request.PageSize)
            }
        };
    }

    public async Task<ApiResponse<ReportDetailResponse>> GetByIdAsync(long reportId, string? userId, CancellationToken ct = default)
    {
        var report = await ReportQuery().FirstOrDefaultAsync(r => r.ReportId == reportId, ct);
        if (report is null || ((!report.IsPublic || report.IsArchived) && report.ReporterId != userId))
            return new ApiResponse<ReportDetailResponse> { Success = false, Message = $"Không tìm thấy Report ID = {reportId}." };
        var reporterName = await _db.Users.Where(u => u.Id == report.ReporterId).Select(u => u.FullName).FirstOrDefaultAsync(ct) ?? "Công dân";
        var hasUpvoted = userId is not null && await _db.ReportUpvotes.AnyAsync(x => x.ReportId == reportId && x.UserId == userId, ct);
        var summary = MapSummary(report, hasUpvoted);
        return new ApiResponse<ReportDetailResponse>
        {
            Success = true,
            Data = new ReportDetailResponse
            {
                Id = summary.Id, PublicCode = summary.PublicCode, Title = summary.Title, Area = summary.Area,
                Latitude = summary.Latitude, Longitude = summary.Longitude, ThumbnailUrl = summary.ThumbnailUrl,
                UpvoteCount = summary.UpvoteCount, HasUpvoted = summary.HasUpvoted, ReportedAt = summary.ReportedAt,
                IssueCount = summary.IssueCount, IssueTypes = summary.IssueTypes, ResolvedIssueCount = summary.ResolvedIssueCount,
                Description = report.Description, AddressText = report.AddressText, ReporterDisplayName = reporterName,
                Attachments = report.Issues.SelectMany(i => i.Attachments).Select(MapAttachment).ToList(),
                Issues = report.Issues.OrderBy(i => i.IssueId).Select(MapIssue).ToList()
            }
        };
    }

    public Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyAsync(FindNearbyIssuesRequest request, string? userId, CancellationToken ct = default)
        => _issues.FindNearbyIssuesAsync(request, userId, ct);

    public async Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetTimelineAsync(long reportId, CancellationToken ct = default)
    {
        var issueIds = await _db.Issues.Where(x => x.ReportId == reportId).Select(x => x.IssueId).ToListAsync(ct);
        if (issueIds.Count == 0) return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>> { Success = false, Message = $"Không tìm thấy Report ID = {reportId}." };
        var items = new List<IssueTimelineItemResponse>();
        foreach (var issueId in issueIds)
        {
            var result = await _issues.GetIssueTimelineAsync(issueId, ct);
            if (result.Data is not null) items.AddRange(result.Data);
        }
        return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>> { Success = true, Data = items.OrderBy(x => x.CreatedAt).ToList() };
    }

    public async Task<ApiResponse<IReadOnlyList<ReportUpdateResponse>>> GetMyRecentUpdatesAsync(string reporterId, int limit, CancellationToken ct = default)
    {
        var updates = await _db.IssueUpdates.Include(u => u.FromStatus).Include(u => u.ToStatus)
            .Include(u => u.Issue).ThenInclude(i => i.Report)
            .Where(u => u.Issue.Report.ReporterId == reporterId)
            .OrderByDescending(u => u.CreatedAt).Take(Math.Clamp(limit, 1, 20)).AsNoTracking().ToListAsync(ct);
        return new ApiResponse<IReadOnlyList<ReportUpdateResponse>>
        {
            Success = true,
            Data = updates.Select(u => new ReportUpdateResponse
            {
                Id = u.Id, ReportId = u.Issue.ReportId, ReportPublicCode = u.Issue.Report.PublicCode,
                ReportTitle = u.Issue.Report.Title, IssueId = u.IssueId, IssuePublicCode = u.Issue.PublicCode,
                UpdateType = u.FromStatusId is null ? "CREATED" : "STATUS_CHANGE",
                FromStatus = u.FromStatus is null ? null : Lookup(u.FromStatus.StatusId, u.FromStatus.StatusName, u.FromStatus.StatusCode),
                ToStatus = Lookup(u.ToStatus.StatusId, u.ToStatus.StatusName, u.ToStatus.StatusCode),
                ProgressPercent = u.ProgressPercent, Note = u.Note, IsSystemGenerated = u.IsSystemGenerated, CreatedAt = u.CreatedAt
            }).ToList()
        };
    }

    public Task<long?> GetPrimaryIssueIdAsync(long reportId, CancellationToken ct = default)
        => _db.Issues.Where(x => x.ReportId == reportId).OrderBy(x => x.IssueId).Select(x => (long?)x.IssueId).FirstOrDefaultAsync(ct);

    private IQueryable<Report> ReportQuery() => _db.Reports
        .Include(r => r.Area)
        .Include(r => r.Issues).ThenInclude(i => i.IssueType)
        .Include(r => r.Issues).ThenInclude(i => i.Priority)
        .Include(r => r.Issues).ThenInclude(i => i.Status)
        .Include(r => r.Issues).ThenInclude(i => i.Attachments)
        .Include(r => r.Issues).ThenInclude(i => i.Assignments).ThenInclude(a => a.Department)
        .AsNoTracking();

    private static ReportSummaryResponse MapSummary(Report report, bool hasUpvoted)
    {
        var attachment = report.Issues.SelectMany(i => i.Attachments).FirstOrDefault();
        return new ReportSummaryResponse
        {
            Id = report.ReportId, PublicCode = report.PublicCode, Title = report.Title,
            Area = Lookup(report.AreaId, report.Area.AreaName, report.Area.AreaCode), Latitude = report.Latitude,
            Longitude = report.Longitude, ThumbnailUrl = attachment?.ThumbnailUrl ?? attachment?.FileUrl,
            UpvoteCount = report.UpvoteCount, HasUpvoted = hasUpvoted, ReportedAt = report.ReportedAt,
            IssueCount = report.Issues.Count,
            IssueTypes = report.Issues.Select(i => Lookup(i.IssueTypeId, i.IssueType.TypeName, i.IssueType.TypeCode)).ToList(),
            ResolvedIssueCount = report.Issues.Count(i => i.Status.IsClosed)
        };
    }

    private static ReportIssueResponse MapIssue(Issue issue) => new()
    {
        Id = issue.IssueId, PublicCode = issue.PublicCode,
        IssueType = Lookup(issue.IssueTypeId, issue.IssueType.TypeName, issue.IssueType.TypeCode),
        Priority = Lookup(issue.PriorityId, issue.Priority.PriorityName, issue.Priority.PriorityCode),
        Status = Lookup(issue.StatusId, issue.Status.StatusName, issue.Status.StatusCode),
        CurrentDepartment = issue.Assignments.Where(a => a.IsCurrent).Select(a => Lookup(a.DepartmentId, a.Department.DepartmentName, a.Department.DepartmentCode)).FirstOrDefault(),
        ReportedAt = issue.ReportedAt, ResolvedAt = issue.ResolvedAt
    };

    private static LookupItemResponse Lookup(int id, string name, string? code) => new() { Id = id, Name = name, Code = code };
    private static IssueAttachmentResponse MapAttachment(IssueAttachment a) => new()
    {
        Id = a.Id, Kind = a.Kind, FileUrl = a.FileUrl, ThumbnailUrl = a.ThumbnailUrl, MimeType = a.MimeType,
        FileSizeBytes = a.FileSizeBytes, WidthPx = a.WidthPx, HeightPx = a.HeightPx, CreatedAt = a.CreatedAt
    };
}
