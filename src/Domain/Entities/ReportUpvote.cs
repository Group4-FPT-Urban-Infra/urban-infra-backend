namespace UrbanInfraSystem.Domain.Entities;

/// <summary>
/// Bảng upvote cho Report (legacy - giữ lại để không break migration).
/// Hiện tại chỉ Issue mới có upvote, Report không còn upvote nữa.
/// </summary>
public class ReportUpvote
{
    /// <summary>Mã report được upvote.</summary>
    public long ReportId { get; set; }

    /// <summary>Mã user thực hiện upvote.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>Thời điểm upvote.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==================== Navigation Properties ====================

    /// <summary>Report được upvote.</summary>
    public Report Report { get; set; } = default!;
}
