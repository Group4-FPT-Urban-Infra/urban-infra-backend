using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedSlaPolicies
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedSlaPolicies(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.SlaPolicies.Any())
        {
            _logger.LogInformation("SlaPolicies already exist, skipping.");
            return;
        }

        var issueTypes = _db.IssueTypes.ToList();
        var priorities = _db.IssuePriorities.ToList();

        if (issueTypes.Count == 0 || priorities.Count == 0)
        {
            _logger.LogWarning("Required reference data missing. Run other seeders first.");
            return;
        }

        // SLA policies: ResolutionMinutes, FirstResponseMinutes
        // CRITICAL: 2h resolve, 15min first response
        // HIGH: 8h resolve, 30min first response
        // MEDIUM: 24h resolve, 2h first response
        // LOW: 72h resolve, 8h first response

        // Different types may have different SLA multipliers
        var policies = new List<SlaPolicy>();

        foreach (var type in issueTypes)
        {
            foreach (var priority in priorities)
            {
                var (resolutionMinutes, firstResponseMinutes) = GetSlaMinutes(type.TypeCode, priority.PriorityCode);

                policies.Add(new SlaPolicy
                {
                    IssueTypeId = type.IssueTypeId,
                    PriorityId = priority.PriorityId,
                    ResolutionMinutes = resolutionMinutes,
                    FirstResponseMinutes = firstResponseMinutes
                });
            }
        }

        _db.SlaPolicies.AddRange(policies);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} SLA policies ({TypeCount} types x {PriorityCount} priorities).",
            policies.Count, issueTypes.Count, priorities.Count);
    }

    private static (int ResolutionMinutes, int FirstResponseMinutes) GetSlaMinutes(string typeCode, string priorityCode)
    {
        var baseResolution = priorityCode switch
        {
            "CRITICAL" => 120,     // 2h resolve
            "HIGH" => 480,         // 8h resolve
            "MEDIUM" => 1440,     // 24h resolve
            "LOW" => 4320,        // 72h resolve
            _ => 1440
        };

        var baseFirstResponse = priorityCode switch
        {
            "CRITICAL" => 15,     // 15min response
            "HIGH" => 30,         // 30min response
            "MEDIUM" => 120,      // 2h response
            "LOW" => 480,         // 8h response
            _ => 120
        };

        // Adjust for specific issue types
        var multiplier = typeCode switch
        {
            // Flood and drainage issues need faster response
            "FLOOD" or "DRAIN" => 0.75m,
            // Road hazards are more urgent
            "POTHOLE" or "ROAD" => 0.8m,
            // Environmental issues can be slightly slower
            "GARBAGE" or "TREE" => 1.25m,
            // Signs are moderate
            "SIGN" => 1.0m,
            // Lights need medium response
            "LIGHT" => 0.9m,
            _ => 1.0m
        };

        return (
            (int)(baseResolution * multiplier),
            (int)(baseFirstResponse * multiplier)
        );
    }
}
