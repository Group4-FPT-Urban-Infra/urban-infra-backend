using System;
using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.DTOs.Staff;

/// <summary>
/// DTO đại diện cho một công việc (sự cố) được giao cho cán bộ xử lý.
/// </summary>
public class StaffTaskResponse
{
    /// <summary>
    /// ID nội bộ của sự cố.
    /// </summary>
    public long IssueId { get; set; }

    /// <summary>
    /// Mã công khai của sự cố (ví dụ: ISS-20240101-ABCDEF).
    /// </summary>
    public string PublicCode { get; set; } = string.Empty;

    /// <summary>
    /// Tiêu đề của sự cố.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái hiện tại của sự cố.
    /// </summary>
    public LookupItemResponse Status { get; set; } = null!;

    /// <summary>
    /// Mức độ ưu tiên của sự cố.
    /// </summary>
    public LookupItemResponse Priority { get; set; } = null!;

    /// <summary>
    /// Khu vực xảy ra sự cố.
    /// </summary>
    public LookupItemResponse Area { get; set; } = null!;

    /// <summary>
    /// Địa chỉ chi tiết do người dân cung cấp.
    /// </summary>
    public string? AddressText { get; set; }

    /// <summary>
    /// Thời điểm báo cáo sự cố.
    /// </summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// Hạn chót phải giải quyết sự cố theo SLA.
    /// </summary>
    public DateTime? SlaResolutionDueAt { get; set; }

    /// <summary>
    /// Cờ báo hiệu sự cố đã vi phạm SLA về thời gian giải quyết.
    /// </summary>
    public bool IsSlaBreached { get; set; }
}