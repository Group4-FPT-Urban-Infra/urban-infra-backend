using System;
using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.DTOs.Staff;

/// <summary>
/// DTO chứa dữ liệu tóm tắt cho dashboard của cán bộ xử lý.
/// </summary>
public class StaffDashboardSummaryResponse
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalOpenIssues { get; set; }
    public int NewIssues { get; set; }
    public int InProgressIssues { get; set; }
    public int RecentlyResolvedIssues { get; set; }
    public int SlaBreachedOpenIssues { get; set; }
}

/// <summary>
/// DTO đại diện cho một công việc (sự cố) được giao cho cán bộ xử lý.
/// </summary>
public class StaffTaskResponse
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public LookupItemResponse Status { get; set; } = null!;
    public LookupItemResponse Priority { get; set; } = null!;
    public LookupItemResponse Area { get; set; } = null!;
    public string? AddressText { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? SlaResolutionDueAt { get; set; }
    public bool IsSlaBreached { get; set; }
}

/// <summary>
/// DTO đại diện cho một hoạt động gần đây liên quan đến công việc của cán bộ.
/// </summary>
public class StaffActivityResponse
{
    public long ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public DateTime ActivityTimestamp { get; set; }
    public long IssueId { get; set; }
    public string IssuePublicCode { get; set; } = string.Empty;
    public string IssueTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActorName { get; set; }
}

/// <summary>
/// DTO cho các tham số lọc và phân trang khi lấy danh sách sự cố cho cán bộ.
/// </summary>
public class StaffIncidentFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    /// <summary>
    /// Bộ lọc theo phạm vi: "my" = chỉ issue của tôi, "department" = issue của đơn vị, "all" = tất cả (mặc định)
    /// </summary>
    public string Scope { get; set; } = "department";
    public string[]? StatusCodes { get; set; }
    public int[]? PriorityIds { get; set; }
    public int[]? IssueTypeIds { get; set; }
    public string? Keyword { get; set; }
}

/// <summary>
/// DTO cho các tham số lọc khi lấy danh sách sự cố hiển thị trên bản đồ.
/// </summary>
public class StaffMapFilterRequest
{
    /// <summary>
    /// Bộ lọc theo phạm vi: "my" = chỉ issue của tôi, "department" = issue của đơn vị, "all" = tất cả (mặc định)
    /// </summary>
    public string Scope { get; set; } = "department";
    public string[]? StatusCodes { get; set; }
    public int[]? PriorityIds { get; set; }
    public int[]? IssueTypeIds { get; set; }
    public string? Keyword { get; set; }
    /// <summary>
    /// Bán kính tìm kiếm tính bằng mét. Nếu có giá trị, sẽ lọc theo vị trí.
    /// </summary>
    public double? RadiusMeters { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// DTO đại diện cho một sự cố trong danh sách quản lý của cán bộ.
/// </summary>
public class StaffIncidentResponse
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public LookupItemResponse Status { get; set; } = null!;
    public LookupItemResponse Priority { get; set; } = null!;
    public LookupItemResponse IssueType { get; set; } = null!;
    public LookupItemResponse Area { get; set; } = null!;
    public DateTime ReportedAt { get; set; }
    public LookupItemResponse? AssignedDepartment { get; set; }
    /// <summary>
    /// Trạng thái phân công của cán bộ hiện tại: PENDING, ACCEPTED, REJECTED, COMPLETED
    /// </summary>
    public string? AssignmentStatus { get; set; }
    public bool IsSlaBreached { get; set; }
}

/// <summary>
/// DTO đại diện cho một sự cố hiển thị trên bản đồ của cán bộ.
/// </summary>
public class StaffMapIssueResponse
{
    public long IssueId { get; set; }
    public string PublicCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public LookupItemResponse Status { get; set; } = null!;
    public LookupItemResponse Priority { get; set; } = null!;
    public LookupItemResponse IssueType { get; set; } = null!;
    public LookupItemResponse? AssignedDepartment { get; set; }
    public bool IsSlaBreached { get; set; }
}