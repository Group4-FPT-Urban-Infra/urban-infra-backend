using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedIssueRelatedData
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedIssueRelatedData(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedIssueSlasAsync();
        await SeedIssueUpvotesAsync();
        await SeedIssueAttachmentsAsync();
    }

    private async Task SeedIssueSlasAsync()
    {
        if (_db.IssueSlas.Any())
        {
            _logger.LogInformation("IssueSlas already exist, skipping.");
            return;
        }

        var issues = _db.Issues.ToList();
        if (issues.Count == 0)
        {
            _logger.LogWarning("No issues found. Run SeedIssues first.");
            return;
        }

        var slaPolicies = _db.SlaPolicies.ToList();
        if (slaPolicies.Count == 0)
        {
            _logger.LogWarning("No SLA policies found. Run SeedSlaPolicies first.");
            return;
        }

        var issueSlas = new List<IssueSla>();

        foreach (var issue in issues)
        {
            // Find matching SLA policy
            var sla = slaPolicies.FirstOrDefault(p =>
                p.IssueTypeId == issue.IssueTypeId && p.PriorityId == issue.PriorityId);

            if (sla == null)
            {
                _logger.LogWarning("No SLA policy for IssueType={TypeId}, Priority={PriorityId}",
                    issue.IssueTypeId, issue.PriorityId);
                continue;
            }

            var firstResponseDueAt = issue.ReportedAt.AddMinutes(sla.FirstResponseMinutes);
            var resolutionDueAt = issue.ReportedAt.AddMinutes(sla.ResolutionMinutes);

            // Determine if breached based on current status
            var isFirstResponseBreached = issue.Status?.StatusCode != "NEW" &&
                issue.Status?.StatusCode != "ASSIGNED" &&
                DateTime.UtcNow > firstResponseDueAt;

            var isResolutionBreached = (issue.ResolvedAt == null && DateTime.UtcNow > resolutionDueAt) ||
                (issue.ResolvedAt.HasValue && issue.ResolvedAt > resolutionDueAt);

            issueSlas.Add(new IssueSla
            {
                IssueId = issue.IssueId,
                SlaPolicyId = sla.Id,
                FirstResponseMinutes = sla.FirstResponseMinutes,
                ResolutionMinutes = sla.ResolutionMinutes,
                FirstResponseDueAt = firstResponseDueAt,
                ResolutionDueAt = resolutionDueAt,
                FirstRespondedAt = isFirstResponseBreached ? null : (DateTime?)null,
                ResolvedAt = issue.ResolvedAt,
                IsFirstResponseBreached = isFirstResponseBreached,
                IsResolutionBreached = isResolutionBreached,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (issueSlas.Count > 0)
        {
            _db.IssueSlas.AddRange(issueSlas);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} issue SLAs.", issueSlas.Count);
        }
    }

    private async Task SeedIssueUpvotesAsync()
    {
        if (_db.ReportUpvotes.Any())
        {
            _logger.LogInformation("ReportUpvotes already exist, skipping.");
            return;
        }

        var issues = _db.Issues.ToList();
        var citizens = _db.Users
            .Where(u => _db.UserRoles.Any(ur => _db.Roles.Any(r => r.Name == "Citizen" && r.Id == ur.RoleId) && ur.UserId == u.Id))
            .ToList();

        if (citizens.Count == 0)
        {
            _logger.LogInformation("No citizen users found. Skipping upvotes seeding.");
            return;
        }

        var upvotes = new List<ReportUpvote>();
        var random = new Random(42);

        foreach (var issue in issues)
        {
            if (issue.UpvoteCount <= 0) continue;

            // Randomly select citizens to upvote this issue
            var upvoters = citizens.OrderBy(_ => random.Next()).Take(Math.Min(issue.UpvoteCount, citizens.Count)).ToList();

            foreach (var voter in upvoters)
            {
                // Check if already upvoted
                if (_db.ReportUpvotes.Any(u => u.ReportId == issue.ReportId && u.UserId == voter.Id))
                    continue;

                upvotes.Add(new ReportUpvote
                {
                    ReportId = issue.ReportId,
                    UserId = voter.Id,
                    CreatedAt = issue.ReportedAt.AddHours(random.Next(1, 48))
                });
            }
        }

        if (upvotes.Count > 0)
        {
            _db.ReportUpvotes.AddRange(upvotes);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} report upvotes.", upvotes.Count);
        }
    }

    private async Task SeedIssueAttachmentsAsync()
    {
        if (_db.IssueAttachments.Any())
        {
            _logger.LogInformation("IssueAttachments already exist, skipping.");
            return;
        }

        var issues = _db.Issues.ToList();
        if (issues.Count == 0)
        {
            _logger.LogWarning("No issues found. Skipping attachments seeding.");
            return;
        }

        // Sample images from wwwroot/uploads/issues (relative paths for seed)
        var sampleImages = new[]
        {
            "uploads/issues/93aeb30e9d2649e6907f34fcc2f0caa8.jpg",
            "uploads/issues/dd97140e92d04fd493fd44a722b88eb4.jpg",
            "uploads/issues/e041d5529a9140289a26d7d02b8b1428.jpg",
            "uploads/issues/e7f0187c698c4cf49ba53c3b316bc956.jpg",
        };

        // Check which images actually exist in wwwroot
        var existingImages = sampleImages.Where(img =>
            File.Exists(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "API", "wwwroot", img.Replace("uploads/issues/", "")))).ToList();

        // If no images found, use placeholder URLs
        var imageUrls = existingImages.Count > 0
            ? existingImages.ToList()
            : new List<string>
            {
                "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800",
                "https://images.unsplash.com/photo-1541888946425-d81bb19240f5?w=800",
                "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=800",
                "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?w=800"
            };

        var attachments = new List<IssueAttachment>();
        var random = new Random(42);

        var updateByIssueId = _db.IssueUpdates
            .GroupBy(u => u.IssueId)
            .ToDictionary(g => g.Key, g => g.OrderBy(u => u.CreatedAt).First());

        foreach (var issue in issues.Where(i => !updateByIssueId.ContainsKey(i.IssueId)))
        {
            var update = new IssueUpdate
            {
                IssueId = issue.IssueId,
                CreatedBy = issue.ReporterId,
                FromStatusId = null,
                ToStatusId = issue.StatusId,
                Note = $"Khởi tạo dữ liệu cho issue {issue.PublicCode}.",
                IsSystemGenerated = true,
                CreatedAt = issue.ReportedAt
            };
            _db.IssueUpdates.Add(update);
            updateByIssueId[issue.IssueId] = update;
        }

        await _db.SaveChangesAsync();

        foreach (var issue in issues)
        {
            // Assign 1-3 random attachments per issue
            var numAttachments = random.Next(1, 4);
            var selectedImages = imageUrls.OrderBy(_ => random.Next()).Take(numAttachments);

            foreach (var imgUrl in selectedImages)
            {
                attachments.Add(new IssueAttachment(updateByIssueId[issue.IssueId].Id)
                {
                    IssueId = issue.IssueId,
                    UploadedBy = issue.ReporterId,
                    Kind = "IMAGE",
                    FileUrl = imgUrl,
                    MimeType = imgUrl.EndsWith(".jpg") || imgUrl.EndsWith(".jpeg")
                        ? "image/jpeg"
                        : "image/png",
                    FileSizeBytes = random.Next(50000, 500000),
                    WidthPx = 800,
                    HeightPx = 600,
                    CreatedAt = issue.ReportedAt.AddMinutes(random.Next(5, 60))
                });
            }
        }

        if (attachments.Count > 0)
        {
            _db.IssueAttachments.AddRange(attachments);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} issue attachments.", attachments.Count);
        }
        else
        {
            _logger.LogInformation("No attachments to seed.");
        }
    }
}
