namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Yêu cầu chuyển tiếp xử lý sự cố từ đơn vị hiện tại sang đơn vị khác.
/// </summary>
public class ReRouteRequest
{
    /// <summary>ID tự tăng.</summary>
    public long Id { get; set; }

    /// <summary>ID của sự cố cần chuyển tiếp.</summary>
    public long IssueId { get; set; }

    /// <summary>ID đơn vị xử lý hiện tại.</summary>
    public int CurrentDepartmentId { get; set; }

    /// <summary>ID đơn vị mục tiêu cần chuyển đến.</summary>
    public int TargetDepartmentId { get; set; }

    /// <summary>Ghi chú/lý do chuyển tiếp.</summary>
    public string? Note { get; set; }

    /// <summary>Trạng thái yêu cầu: Pending, Accepted, Cancelled, Rejected.</summary>
    public ReRouteStatus Status { get; set; } = ReRouteStatus.Pending;

    /// <summary>User ID của người yêu cầu chuyển.</summary>
    public string RequestedBy { get; set; } = default!;

    /// <summary>Thời điểm gửi yêu cầu.</summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm xử lý yêu cầu.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>User ID của người xử lý (Admin).</summary>
    public string? ProcessedBy { get; set; }

    // ==================== Navigation Properties ====================

    /// <summary>Sự cố được chuyển tiếp.</summary>
    public Issue Issue { get; set; } = default!;

    /// <summary>Đơn vị xử lý hiện tại.</summary>
    public Department CurrentDepartment { get; set; } = default!;

    /// <summary>Đơn vị mục tiêu.</summary>
    public Department TargetDepartment { get; set; } = default!;
}

/// <summary>
/// Trạng thái yêu cầu chuyển tiếp xử lý.
/// </summary>
public enum ReRouteStatus
{
    /// <summary>Đang chờ xử lý.</summary>
    Pending,

    /// <summary>Đã chấp nhận và chuyển.</summary>
    Accepted,

    /// <summary>Đã hủy bởi người yêu cầu.</summary>
    Cancelled,

    /// <summary>Bị từ chối.</summary>
    Rejected
}
