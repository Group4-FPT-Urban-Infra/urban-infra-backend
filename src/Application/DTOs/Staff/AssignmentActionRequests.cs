namespace UrbanInfraSystem.Application.DTOs.Staff;

/// <summary>
/// Request body cho việc từ chối phân công.
/// </summary>
public class RejectAssignmentRequest
{
    /// <summary>
    /// Lý do từ chối (tùy chọn).
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Request body cho việc đánh dấu hoàn thành phân công.
/// </summary>
public class CompleteAssignmentRequest
{
    /// <summary>
    /// Ghi chú khi hoàn thành (tùy chọn).
    /// </summary>
    public string? Note { get; set; }
}
