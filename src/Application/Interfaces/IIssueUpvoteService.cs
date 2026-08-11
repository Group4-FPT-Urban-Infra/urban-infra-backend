using UrbanInfraSystem.Application.DTOs.Issues;

namespace UrbanInfraSystem.Application.Interfaces;

/// <summary>
/// Service interface cho nghiệp vụ upvote sự cố.
/// </summary>
public interface IIssueUpvoteService
{
    /// <summary>
    /// Upvote một sự cố. Gọi lặp lại không tạo upvote thứ hai (idempotent).
    /// </summary>
    /// <param name="issueId">ID của sự cố cần upvote.</param>
    /// <param name="userId">ID của user thực hiện upvote.</param>
    /// <returns>Trạng thái upvote hiện tại của sự cố.</returns>
    Task<ApiResponse<UpvoteResponse>> UpvoteAsync(long issueId, string userId);

    /// <summary>
    /// Bỏ upvote một sự cố. Gọi lặp lại vẫn trả trạng thái hiện tại (idempotent).
    /// </summary>
    /// <param name="issueId">ID của sự cố cần bỏ upvote.</param>
    /// <param name="userId">ID của user thực hiện bỏ upvote.</param>
    /// <returns>Trạng thái upvote hiện tại của sự cố.</returns>
    Task<ApiResponse<UpvoteResponse>> RemoveUpvoteAsync(long issueId, string userId);

    /// <summary>
    /// Lấy trạng thái upvote của một sự cố cho user hiện tại.
    /// </summary>
    /// <param name="issueId">ID của sự cố.</param>
    /// <param name="userId">ID của user cần kiểm tra.</param>
    /// <returns>Trạng thái upvote của sự cố.</returns>
    Task<ApiResponse<UpvoteResponse>> GetUpvoteStatusAsync(long issueId, string? userId);
}
