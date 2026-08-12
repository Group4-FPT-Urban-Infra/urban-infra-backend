using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedIssuePriorities
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedIssuePriorities(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.IssuePriorities.Any())
        {
            _logger.LogInformation("IssuePriorities already exist, skipping.");
            return;
        }

        var priorities = new List<IssuePriority>
        {
            new()
            {
                PriorityCode = "CRITICAL",
                PriorityName = "Nghiêm trọng",
                SeverityRank = 1,
                IsActive = true
            },
            new()
            {
                PriorityCode = "HIGH",
                PriorityName = "Cao",
                SeverityRank = 2,
                IsActive = true
            },
            new()
            {
                PriorityCode = "MEDIUM",
                PriorityName = "Trung bình",
                SeverityRank = 3,
                IsActive = true
            },
            new()
            {
                PriorityCode = "LOW",
                PriorityName = "Thấp",
                SeverityRank = 4,
                IsActive = true
            }
        };

        _db.IssuePriorities.AddRange(priorities);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} issue priorities.", priorities.Count);
    }
}
