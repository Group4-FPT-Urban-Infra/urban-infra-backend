using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedRoutingRules
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedRoutingRules(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.RoutingRules.Any())
        {
            _logger.LogInformation("RoutingRules already exist, skipping.");
            return;
        }

        var departments = _db.Departments.ToList();
        var areas = _db.Areas.ToList();
        var issueTypes = _db.IssueTypes.ToList();

        if (departments.Count == 0 || areas.Count == 0 || issueTypes.Count == 0)
        {
            _logger.LogWarning("Required reference data missing. Run other seeders first.");
            return;
        }

        // Get root issue types only (ParentIssueTypeId == null)
        var rootTypes = issueTypes.Where(t => t.ParentIssueTypeId == null).ToList();

        // Get district-level areas (AreaType == "District")
        var districts = areas.Where(a => a.AreaType == "District").ToList();

        // Routing: IssueType (root) + District → Department
        // QLDT  : LIGHT, POTHOLE, ROAD
        // TN    : DRAIN, FLOOD
        // MT    : GARBAGE
        // GT    : SIGN
        // VSDN  : TREE
        // MC    : handles all types in Móng Cái district
        var typeToDeptCode = new Dictionary<string, string>
        {
            ["LIGHT"]   = "QLDT",
            ["POTHOLE"] = "QLDT",
            ["ROAD"]    = "QLDT",
            ["DRAIN"]   = "TN",
            ["FLOOD"]   = "TN",
            ["GARBAGE"] = "MT",
            ["SIGN"]    = "GT",
            ["TREE"]    = "VSDN",
        };

        var rules = new List<RoutingRule>();

        foreach (var district in districts)
        {
            foreach (var rootType in rootTypes)
            {
                if (!typeToDeptCode.TryGetValue(rootType.TypeCode, out var deptCode))
                    continue;

                // Special case: Móng Cái district routes everything to MC department
                if (district.AreaCode == "MC")
                    deptCode = "MC";

                var dept = departments.FirstOrDefault(d => d.DepartmentCode == deptCode);
                if (dept == null) continue;

                if (_db.RoutingRules.Any(r =>
                    r.IssueTypeId == rootType.IssueTypeId &&
                    r.AreaId == district.AreaId &&
                    r.DepartmentId == dept.DepartmentId))
                    continue;

                rules.Add(new RoutingRule
                {
                    IssueTypeId = rootType.IssueTypeId,
                    AreaId = district.AreaId,
                    DepartmentId = dept.DepartmentId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (rules.Count > 0)
        {
            _db.RoutingRules.AddRange(rules);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} routing rules ({Districts} districts × {Types} root types).",
                rules.Count, districts.Count, rootTypes.Count);
        }
    }
}
