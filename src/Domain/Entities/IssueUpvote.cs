namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Entity cho bảng upvote sự cố. Mỗi bản ghi đại diện cho một công dân upvote một sự cố.
/// Ràng buộc: 1 user chỉ upvote 1 lần cho 1 issue (composite PK).
/// </summary>
public class IssueUpvote
{
    /// <summary>ID của sự cố được upvote.</summary>
    public long IssueId { get; set; }

    /// <summary>ID của user thực hiện upvote.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>Thời điểm upvote.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Sự cố được upvote.</summary>
    public Issue Issue { get; set; } = default!;
}
