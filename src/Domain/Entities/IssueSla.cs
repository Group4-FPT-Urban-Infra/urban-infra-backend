namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Entity lưu vết SLA áp dụng cho từng Sự cố (Issue).
/// Tự động sinh ra khi sự cố được tạo dựa trên SLA Policy (Type + Priority).
/// </summary>
public class IssueSla
{
    /// <summary>ID tự tăng của Issue SLA.</summary>
    public long Id { get; set; }

    /// <summary>ID sự cố tương ứng.</summary>
    public long IssueId { get; set; }

    /// <summary>ID chính sách SLA áp dụng (nullable nếu dùng mặc định).</summary>
    public Guid? SlaPolicyId { get; set; }

    /// <summary>Thời gian phản hồi đầu tiên cam kết (phút).</summary>
    public int FirstResponseMinutes { get; set; }

    /// <summary>Thời gian xử lý xong cam kết (phút).</summary>
    public int ResolutionMinutes { get; set; }

    /// <summary>Hạn chót phản hồi đầu tiên (first_response_due_at).</summary>
    public DateTime? FirstResponseDueAt { get; set; }

    /// <summary>Hạn chót giải quyết sự cố (resolution_due_at).</summary>
    public DateTime ResolutionDueAt { get; set; }

    /// <summary>Thời điểm phản hồi đầu tiên thực tế.</summary>
    public DateTime? FirstRespondedAt { get; set; }

    /// <summary>Thời điểm giải quyết thực tế.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Đánh dấu đã vi phạm hạn phản hồi đầu tiên hay chưa.</summary>
    public bool IsFirstResponseBreached { get; set; } = false;

    /// <summary>Đánh dấu đã vi phạm hạn giải quyết hay chưa.</summary>
    public bool IsResolutionBreached { get; set; } = false;

    /// <summary>Thời điểm tạo bản ghi SLA.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Sự cố tương ứng.</summary>
    public Issue Issue { get; set; } = default!;

    /// <summary>Chính sách SLA tương ứng (nếu có).</summary>
    public SlaPolicy? SlaPolicy { get; set; }
}
