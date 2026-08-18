namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>
/// Service xử lý upvote/unvote cho Issue.
/// </summary>
public interface IIssueUpvoteService
{
    /// <summary>
    /// Toggle upvote cho một Issue. Nếu chưa upvote thì thêm, nếu đã upvote thì xóa.
    /// </summary>
    /// <param name="issueId">Mã Issue</param>
    /// <param name="userId">Mã user thực hiện</param>
    /// <param name="cancellationToken">Token hủy</param>
    /// <returns>True nếu sau thao tác issue đang được upvote, False nếu đã bỏ upvote</returns>
    Task<bool> ToggleAsync(long issueId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa upvote khỏi một Issue.
    /// </summary>
    /// <param name="issueId">Mã Issue</param>
    /// <param name="userId">Mã user thực hiện</param>
    /// <param name="cancellationToken">Token hủy</param>
    Task RemoveAsync(long issueId, string userId, CancellationToken cancellationToken = default);
}
