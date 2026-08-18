namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Bảng upvote cho Issue. Mỗi user có thể upvote 1 issue 1 lần.
/// </summary>
public class IssueUpvote
{
    /// <summary>Mã issue được upvote.</summary>
    public long IssueId { get; set; }

    /// <summary>Mã user thực hiện upvote.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>Thời điểm upvote.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Issue được upvote.</summary>
    public Issue Issue { get; set; } = default!;
}
