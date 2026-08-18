using System;
using UrbanInfraSystem.Domain.Common;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Entity cho sự cố hạ tầng đô thị.
/// </summary>
public class Issue
{
    /// <summary>ID tự tăng của sự cố.</summary>
    public long IssueId { get; set; }

    /// <summary>Phản ánh gốc của công dân chứa Issue này.</summary>
    public long ReportId { get; set; }

    /// <summary>Mô tả loại tự do khi issue_type là OTHER.</summary>
    public string? CustomTypeDescription { get; set; }

    /// <summary>Mã công khai để người dân tra cứu (VD: ISS-2026-000001).</summary>
    public string PublicCode { get; set; } = default!;

    /// <summary>ID của công dân tạo báo cáo.</summary>
    public string ReporterId { get; set; } = default!;

    /// <summary>Loại sự cố (VD: Đèn đường, Ổ gà...).</summary>
    public int IssueTypeId { get; set; }

    /// <summary>Khu vực xảy ra sự cố.</summary>
    public int AreaId { get; set; }

    /// <summary>Mức ưu tiên của sự cố.</summary>
    public int PriorityId { get; set; }

    /// <summary>Trạng thái xử lý hiện tại.</summary>
    public int StatusId { get; set; }

    /// <summary>Tiêu đề ngắn gọn mô tả sự cố.</summary>
    public string Title { get; set; } = default!;

    /// <summary>Mô tả chi tiết sự cố.</summary>
    public string Description { get; set; } = default!;

    /// <summary>Địa chỉ văn bản (geocoding ngược).</summary>
    public string? AddressText { get; set; }

    /// <summary>Vĩ độ của vị trí sự cố.</summary>
    public decimal Latitude { get; set; }

    /// <summary>Kinh độ của vị trí sự cố.</summary>
    public decimal Longitude { get; set; }

    /// <summary>Điểm GEOMETRY lưu trữ vị trí (dùng cho spatial query).</summary>
    // public NetTopologySuite.Geometries.Point? LocationPoint { get; set; }

    /// <summary>Đánh dấu sự cố có hiển thị công khai trên bản đồ không.</summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>Đánh dấu sự cố đã bị ẩn (archive).</summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>Thời điểm công dân tạo báo cáo.</summary>
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm sự cố được giải quyết.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Thời điểm sự cố được đóng.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>ID của sự cố gốc nếu báo cáo này bị đánh dấu trùng.</summary>
    public long? DuplicateOfIssueId { get; set; }

    /// <summary>Số lượt upvote của sự cố này.</summary>
    public int UpvoteCount { get; set; } = 0;

    // ==================== Navigation Properties ====================

    /// <summary>Phản ánh gốc.</summary>
    public Report Report { get; set; } = default!;

    /// <summary>Loại sự cố.</summary>
    public IssueType IssueType { get; set; } = default!;

    /// <summary>Khu vực.</summary>
    public Area Area { get; set; } = default!;

    /// <summary>Mức ưu tiên.</summary>
    public IssuePriority Priority { get; set; } = default!;

    /// <summary>Trạng thái hiện tại.</summary>
    public IssueStatus Status { get; set; } = default!;

    /// <summary>Danh sách đính kèm (hình ảnh trước/sau xử lý).</summary>
    public ICollection<IssueAttachment> Attachments { get; set; } = new List<IssueAttachment>();

    /// <summary>Lịch sử cập nhật trạng thái.</summary>
    public ICollection<IssueUpdate> Updates { get; set; } = new List<IssueUpdate>();

    /// <summary>Lịch sử phân công đơn vị xử lý.</summary>
    public ICollection<IssueAssignment> Assignments { get; set; } = new List<IssueAssignment>();

    /// <summary>Thông tin SLA của sự cố.</summary>
    public IssueSla? Sla { get; set; }

    /// <summary>Danh sách upvote của sự cố.</summary>
    public ICollection<IssueUpvote> Upvotes { get; set; } = new List<IssueUpvote>();
}
