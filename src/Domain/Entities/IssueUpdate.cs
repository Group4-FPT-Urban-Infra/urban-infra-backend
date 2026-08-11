namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Entity cho lịch sử cập nhật trạng thái sự cố.
/// Mỗi bản ghi đại diện cho một thay đổi trạng thái hoặc cập nhật tiến độ.
/// </summary>
public class IssueUpdate
{
    /// <summary>ID tự tăng của update.</summary>
    public long Id { get; set; }

    /// <summary>ID của sự cố được cập nhật.</summary>
    public long IssueId { get; set; }

    /// <summary>ID của user thực hiện cập nhật (Staff hoặc System).</summary>
    public string CreatedBy { get; set; } = default!;

    /// <summary>Trạng thái trước khi thay đổi (nullable cho lần đầu tiên).</summary>
    public int? FromStatusId { get; set; }

    /// <summary>Trạng thái sau khi thay đổi.</summary>
    public int ToStatusId { get; set; }

    /// <summary>Ghi chú/cập nhật tiến độ.</summary>
    public string? Note { get; set; }

    /// <summary>Phần trăm tiến độ hoàn thành (0-100).</summary>
    public byte? ProgressPercent { get; set; }

    /// <summary>Đánh dấu update có phải do hệ thống tự động tạo không.</summary>
    public bool IsSystemGenerated { get; set; } = false;

    /// <summary>Thời điểm tạo update.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Sự cố được cập nhật.</summary>
    public Issue Issue { get; set; } = default!;

    /// <summary>Trạng thái trước.</summary>
    public IssueStatus? FromStatus { get; set; }

    /// <summary>Trạng thái sau.</summary>
    public IssueStatus ToStatus { get; set; } = default!;

    /// <summary>Danh sách đính kèm của update.</summary>
    public ICollection<IssueAttachment> Attachments { get; set; } = new List<IssueAttachment>();
}
