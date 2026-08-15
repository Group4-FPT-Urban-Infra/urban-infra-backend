using Microsoft.EntityFrameworkCore;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

/// <summary>
/// Service implementation cho nghiệp vụ upvote sự cố.
/// Xử lý idempotent: upvote lặp lại không tạo bản ghi trùng, remove upvote lặp lại không lỗi.
/// </summary>
public class IssueUpvoteService : IIssueUpvoteService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IssueUpvoteService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UpvoteResponse>> UpvoteAsync(long issueId, string userId)
    {
        // 1. Kiểm tra sự cố tồn tại
        var issue = await _db.Issues
            .Include(i => i.Report)
            .FirstOrDefaultAsync(i => i.IssueId == issueId);

        if (issue == null)
        {
            return new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = $"Không tìm thấy sự cố với ID '{issueId}'."
            };
        }

        // 2. Kiểm tra đã upvote chưa (idempotent)
        var existingUpvote = await _db.ReportUpvotes
            .FirstOrDefaultAsync(u => u.ReportId == issue.ReportId && u.UserId == userId);

        if (existingUpvote != null)
        {
            // Đã upvote rồi, trả về trạng thái hiện tại
            return await BuildUpvoteResponseAsync(issueId, userId);
        }

        // 3. Tạo upvote mới
        var upvote = new ReportUpvote
        {
            ReportId = issue.ReportId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ReportUpvotes.Add(upvote);

        // 4. Tăng upvote_count trên Issue
        issue.Report.UpvoteCount = await _db.ReportUpvotes
            .CountAsync(u => u.ReportId == issue.ReportId) + 1;
        issue.UpvoteCount = issue.Report.UpvoteCount; // compatibility for existing readers

        await _db.SaveChangesAsync();

        return new ApiResponse<UpvoteResponse>
        {
            Success = true,
            Message = "Upvote thành công.",
            Data = new UpvoteResponse
            {
                IssueId = issueId,
                HasUpvoted = true,
                UpvoteCount = issue.UpvoteCount
            }
        };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UpvoteResponse>> RemoveUpvoteAsync(long issueId, string userId)
    {
        // 1. Kiểm tra sự cố tồn tại
        var issue = await _db.Issues
            .Include(i => i.Report)
            .FirstOrDefaultAsync(i => i.IssueId == issueId);

        if (issue == null)
        {
            return new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = $"Không tìm thấy sự cố với ID '{issueId}'."
            };
        }

        // 2. Kiểm tra đã upvote chưa
        var existingUpvote = await _db.ReportUpvotes
            .FirstOrDefaultAsync(u => u.ReportId == issue.ReportId && u.UserId == userId);

        if (existingUpvote == null)
        {
            // Chưa upvote, trả về trạng thái hiện tại (idempotent)
            return await BuildUpvoteResponseAsync(issueId, userId);
        }

        // 3. Xóa upvote
        _db.ReportUpvotes.Remove(existingUpvote);

        // 4. Giảm upvote_count trên Issue
        issue.Report.UpvoteCount = Math.Max(0, await _db.ReportUpvotes
            .CountAsync(u => u.ReportId == issue.ReportId) - 1);
        issue.UpvoteCount = issue.Report.UpvoteCount;

        await _db.SaveChangesAsync();

        return new ApiResponse<UpvoteResponse>
        {
            Success = true,
            Message = "Đã bỏ upvote.",
            Data = new UpvoteResponse
            {
                IssueId = issueId,
                HasUpvoted = false,
                UpvoteCount = issue.UpvoteCount
            }
        };
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UpvoteResponse>> GetUpvoteStatusAsync(long issueId, string? userId)
    {
        // Kiểm tra sự cố tồn tại
        var issueExists = await _db.Issues.AnyAsync(i => i.IssueId == issueId);

        if (!issueExists)
        {
            return new ApiResponse<UpvoteResponse>
            {
                Success = false,
                Message = $"Không tìm thấy sự cố với ID '{issueId}'."
            };
        }

        return await BuildUpvoteResponseAsync(issueId, userId);
    }

    /// <summary>
    /// Xây dựng response upvote từ database.
    /// </summary>
    private async Task<ApiResponse<UpvoteResponse>> BuildUpvoteResponseAsync(long issueId, string? userId)
    {
        var reportId = await _db.Issues
            .Where(i => i.IssueId == issueId)
            .Select(i => i.ReportId)
            .SingleAsync();

        var upvoteCount = await _db.ReportUpvotes
            .CountAsync(u => u.ReportId == reportId);

        var hasUpvoted = !string.IsNullOrEmpty(userId) && await _db.ReportUpvotes
            .AnyAsync(u => u.ReportId == reportId && u.UserId == userId);

        return new ApiResponse<UpvoteResponse>
        {
            Success = true,
            Data = new UpvoteResponse
            {
                IssueId = issueId,
                HasUpvoted = hasUpvoted,
                UpvoteCount = upvoteCount
            }
        };
    }
}
