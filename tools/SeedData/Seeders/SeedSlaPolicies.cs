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

        // Get root issue types only
        var rootTypes = issueTypes.Where(t => t.ParentIssueTypeId == null).ToList();

        var policies = new List<SlaPolicy>();

        foreach (var type in rootTypes)
        {
            foreach (var priority in priorities)
            {
                var (resolutionMinutes, firstResponseMinutes) = GetSlaMinutes(type.TypeCode, priority.PriorityCode);
                // Warn at 50% of first response time
                var warningMinutes = (int)(firstResponseMinutes * 0.5);

                policies.Add(new SlaPolicy
                {
                    IssueTypeId = type.IssueTypeId,
                    PriorityId = priority.PriorityId,
                    ResolutionMinutes = resolutionMinutes,
                    FirstResponseMinutes = firstResponseMinutes,
                    WarningBeforeMinutes = warningMinutes
                });
            }
        }

        _db.SlaPolicies.AddRange(policies);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} SLA policies ({TypeCount} root types × {PriorityCount} priorities).",
            policies.Count, rootTypes.Count, priorities.Count);
    }

    private static (int ResolutionMinutes, int FirstResponseMinutes) GetSlaMinutes(string typeCode, string priorityCode)
    {
        var baseResolution = priorityCode switch
        {
            "CRITICAL" => 75,    // 75min base (LIGHT+CRITICAL = 5m, else 2h)
            "HIGH"     => 480,   // 8h resolve
            "MEDIUM"   => 1440,  // 24h resolve
            "LOW"      => 4320, // 72h resolve
            _          => 1440
        };

        var baseFirstResponse = priorityCode switch
        {
            "CRITICAL" => 15,   // 15min response
            "HIGH"     => 30,   // 30min response
            "MEDIUM"   => 120,  // 2h response
            "LOW"      => 480,  // 8h response
            _          => 120
        };

        // Type-specific multipliers
        var multiplier = typeCode switch
        {
            "FLOOD" or "DRAIN" => 0.75m,   // Flood/drainage — faster
            "POTHOLE" or "ROAD" => 0.8m,  // Road hazards — urgent
            "GARBAGE" or "TREE"  => 1.25m, // Environmental — slower
            "SIGN"                => 1.0m,  // Baseline
            "LIGHT"              => priorityCode == "CRITICAL" ? 0.067m : 0.9m, // Critical LIGHT: 1min response / 5min resolve
            _                    => 1.0m
        };

        return (
            (int)(baseResolution * multiplier),
            (int)(baseFirstResponse * multiplier)
        );
    }
}
