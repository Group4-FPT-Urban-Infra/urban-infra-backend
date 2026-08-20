namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Bảng trung gian cho quan hệ N-N giữa Report và IssueType.
/// Một Report có thể có nhiều loại sự cố (VD: đổ cây + hỏng cột điện).
/// </summary>
public class ReportIssueType
{
    /// <summary>Mã báo cáo gốc.</summary>
    public long ReportId { get; set; }

    /// <summary>Mã loại sự cố.</summary>
    public int IssueTypeId { get; set; }

    /// <summary>Tên loại sự cố tại thời điểm tạo (denormalized).</summary>
    public string IssueTypeName { get; set; } = default!;

    /// <summary>Mã loại sự cố tại thời điểm tạo (denormalized).</summary>
    public string IssueTypeCode { get; set; } = default!;

    /// <summary>Thời điểm liên kết được tạo.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Báo cáo gốc.</summary>
    public Report Report { get; set; } = default!;

    /// <summary>Loại sự cố.</summary>
    public IssueType IssueType { get; set; } = default!;
}
