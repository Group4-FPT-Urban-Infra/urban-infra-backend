using System;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Thực thể lưu thông báo gửi cho người dùng trong hệ thống (gồm cả công dân và cán bộ/quản lý).
/// </summary>
public class Notification
{
    public long Id { get; set; }

    /// <summary>
    /// Id của người dùng nhận thông báo (Identity User Id).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tiêu đề thông báo.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung thông báo chi tiết.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Loại thông báo ("ASSIGNMENT", "STATUS_CHANGE", "ESCALATION", "CUSTOM").
    /// </summary>
    public string NotificationType { get; set; } = "CUSTOM";

    /// <summary>
    /// Id sự cố liên quan (nếu có).
    /// </summary>
    public long? IssueId { get; set; }

    /// <summary>
    /// Trạng thái đã đọc hay chưa.
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Thời điểm tạo thông báo (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm người dùng đọc thông báo (UTC).
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
