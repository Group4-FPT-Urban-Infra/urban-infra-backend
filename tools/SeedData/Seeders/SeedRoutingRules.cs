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

        // Map issue types to departments
        // QLDT: LIGHT, ROAD, POTHOLE
        // CSGT: DRAIN, FLOOD
        // MT: GARBAGE
        // GT: SIGN
        // VSDN: TREE
        var routingMappings = new List<(string TypeCode, string DeptCode, string AreaCode)>
        {
            // QLDT - Quản lý đô thị handles light, road, pothole, sidewalk issues
            ("LIGHT", "QLDT", "HL"),
            ("LIGHT", "QLDT", "HG"),
            ("LIGHT", "QLDT", "BC"),
            ("ROAD", "QLDT", "HL"),
            ("ROAD", "QLDT", "HG"),
            ("ROAD", "QLDT", "BC"),
            ("POTHOLE", "QLDT", "HL"),
            ("POTHOLE", "QLDT", "HG"),
            ("POTHOLE", "QLDT", "BC"),

            // CSGT - Công ty Thoát nước handles drain and flood issues
            ("DRAIN", "CSGT", "HL"),
            ("DRAIN", "CSGT", "HG"),
            ("DRAIN", "CSGT", "BC"),
            ("FLOOD", "CSGT", "HL"),
            ("FLOOD", "CSGT", "HG"),
            ("FLOOD", "CSGT", "BC"),

            // MT - Môi trường handles garbage
            ("GARBAGE", "MT", "HL"),
            ("GARBAGE", "MT", "HG"),
            ("GARBAGE", "MT", "BC"),

            // GT - Giao thông handles signs
            ("SIGN", "GT", "HL"),
            ("SIGN", "GT", "HG"),
            ("SIGN", "GT", "BC"),

            // VSDN - Vệ sinh đô thị handles tree maintenance
            ("TREE", "VSDN", "HL"),
            ("TREE", "VSDN", "HG"),
            ("TREE", "VSDN", "BC"),
        };

        var rules = new List<RoutingRule>();

        foreach (var (typeCode, deptCode, areaCode) in routingMappings)
        {
            var issueType = issueTypes.FirstOrDefault(t => t.TypeCode == typeCode);
            var dept = departments.FirstOrDefault(d => d.DepartmentCode == deptCode);
            var area = areas.FirstOrDefault(a => a.AreaCode == areaCode);

            if (issueType == null || dept == null || area == null)
            {
                _logger.LogWarning("Could not find mapping: Type={Type}, Dept={Dept}, Area={Area}",
                    typeCode, deptCode, areaCode);
                continue;
            }

            // Check if rule already exists for this combination
            if (_db.RoutingRules.Any(r => r.IssueTypeId == issueType.IssueTypeId
                && r.AreaId == area.AreaId && r.DepartmentId == dept.DepartmentId))
            {
                continue;
            }

            rules.Add(new RoutingRule
            {
                IssueTypeId = issueType.IssueTypeId,
                AreaId = area.AreaId,
                DepartmentId = dept.DepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (rules.Count > 0)
        {
            _db.RoutingRules.AddRange(rules);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} routing rules.", rules.Count);
        }
        else
        {
            _logger.LogInformation("No new routing rules to seed.");
        }
    }
}
