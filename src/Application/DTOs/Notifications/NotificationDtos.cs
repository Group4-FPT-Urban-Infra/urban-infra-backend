using System;
using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.Notifications;

public class CreateNotificationRequest
{
    [Required(ErrorMessage = "UserId là bắt buộc.")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tiêu đề thông báo là bắt buộc.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung thông báo là bắt buộc.")]
    [MaxLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự.")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string NotificationType { get; set; } = "CUSTOM";

    public long? IssueId { get; set; }
}

public class NotificationResponse
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public long? IssueId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
