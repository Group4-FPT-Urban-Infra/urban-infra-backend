using System;
using UrbanInfraSystem.Domain.Common;
using UrbanInfraSystem.Domain.Enums;

namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Escalation rule linking a SLA policy and an overdue threshold to a target department or role.
/// </summary>
public class EscalationRule : BaseEntity
{
    public Guid SlaPolicyId { get; set; }

    /// <summary>
    /// Số phút quá hạn (dương) hoặc số phút trước deadline (âm) để trigger escalation.
    /// </summary>
    public int OverdueMinutes { get; set; }

    /// <summary>
    /// Loại escalation trigger: FirstResponseOverdue, ApproachResponseDeadline, ResponseOverdue.
    /// </summary>
    public EscalationType EscalationType { get; set; }

    /// <summary>
    /// Cấp escalation (1 = cấp đầu tiên, 2 = cấp tiếp theo, ...).
    /// </summary>
    public int EscalationLevel { get; set; } = 1;

    /// <summary>
    /// Phòng ban nhận thông báo. Nếu null, gửi đến manager của phòng ban hiện tại của issue.
    /// </summary>
    public int? TargetDepartmentId { get; set; }

    /// <summary>
    /// Role nhận thông báo: "Admin" → gửi đến Admin, "DepartmentManager" → gửi đến manager của TargetDepartmentId.
    /// </summary>
    public string? TargetRoleName { get; set; }

    /// <summary>
    /// Tiêu đề thông báo tùy chỉnh.
    /// </summary>
    public string? NotificationTitle { get; set; }

    /// <summary>
    /// Nội dung template thông báo.
    /// </summary>
    public string? NotificationTemplate { get; set; }

    /// <summary>
    /// Rule có đang active không.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public SlaPolicy? SlaPolicy { get; set; }
    public Department? TargetDepartment { get; set; }
}
