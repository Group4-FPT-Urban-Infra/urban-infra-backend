using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

/// <summary>
/// Service xử lý upvote/unvote cho Issue.
/// </summary>
public class IssueUpvoteService : IIssueUpvoteService
{
    private readonly AppDbContext _db;
    private readonly ILogger<IssueUpvoteService> _logger;

    public IssueUpvoteService(AppDbContext db, ILogger<IssueUpvoteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> ToggleAsync(long issueId, string userId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.IssueUpvotes
            .FirstOrDefaultAsync(u => u.IssueId == issueId && u.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            _db.IssueUpvotes.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            // Decrement counter
            var issue = await _db.Issues.FindAsync([issueId], cancellationToken);
            if (issue is not null)
            {
                issue.UpvoteCount = Math.Max(0, issue.UpvoteCount - 1);
                await _db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("User {UserId} removed upvote from Issue {IssueId}", userId, issueId);
            return false;
        }

        var upvote = new IssueUpvote
        {
            IssueId = issueId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.IssueUpvotes.Add(upvote);

        // Increment counter
        var target = await _db.Issues.FindAsync([issueId], cancellationToken);
        if (target is not null)
        {
            target.UpvoteCount++;
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("User {UserId} upvoted Issue {IssueId}", userId, issueId);
        return true;
    }

    public async Task RemoveAsync(long issueId, string userId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.IssueUpvotes
            .FirstOrDefaultAsync(u => u.IssueId == issueId && u.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            _db.IssueUpvotes.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);

            var issue = await _db.Issues.FindAsync([issueId], cancellationToken);
            if (issue is not null)
            {
                issue.UpvoteCount = Math.Max(0, issue.UpvoteCount - 1);
                await _db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("User {UserId} removed upvote from Issue {IssueId}", userId, issueId);
        }
    }
}
