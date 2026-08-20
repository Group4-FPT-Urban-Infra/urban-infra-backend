using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedIssueAssignments
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedIssueAssignments(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.IssueAssignments.Any())
        {
            _logger.LogInformation("IssueAssignments already exist, skipping.");
            return;
        }

        var issues = _db.Issues
            .Include(i => i.Status)
            .Include(i => i.IssueType)
            .ToList();

        if (issues.Count == 0)
        {
            _logger.LogWarning("No issues found. Run SeedIssues first.");
            return;
        }

        var departments = _db.Departments.ToList();
        if (departments.Count == 0)
        {
            _logger.LogWarning("No departments found. Run SeedDepartments first.");
            return;
        }

        // Staff members (non-managers) per department
        var staffMembers = _db.DepartmentMembers
            .Where(dm => !dm.IsManager && dm.IsActive)
            .ToList();

        var random = new Random(42);
        var created = 0;

        foreach (var issue in issues)
        {
            // Find routing rule for this issue's type (root type) and its area's parent district
            var districtArea = _db.Areas
                .FirstOrDefault(a => a.AreaId == issue.AreaId);

            // If issue is at ward level, get district; if at district level, use itself
            int lookupAreaId;
            if (districtArea?.ParentAreaId != null)
                lookupAreaId = districtArea.ParentAreaId.Value;
            else
                lookupAreaId = issue.AreaId;

            // Get root issue type for routing
            var rootType = _db.IssueTypes
                .FirstOrDefault(t => t.IssueTypeId == issue.IssueTypeId);
            while (rootType?.ParentIssueTypeId != null)
                rootType = _db.IssueTypes.First(t => t.IssueTypeId == rootType.ParentIssueTypeId);

            RoutingRule? rule = null;
            if (rootType != null)
            {
                rule = _db.RoutingRules
                    .FirstOrDefault(r => r.IssueTypeId == rootType.IssueTypeId && r.AreaId == lookupAreaId);
            }

            // Fallback: match by district area code
            if (rule == null)
            {
                var districtCode = _db.Areas.FirstOrDefault(a => a.AreaId == lookupAreaId)?.AreaCode;
                if (districtCode == "MC")
                    rule = _db.RoutingRules.FirstOrDefault(r =>
                        r.AreaId == lookupAreaId &&
                        _db.IssueTypes.Any(t => t.IssueTypeId == r.IssueTypeId));
            }

            // Find department from rule or default to QLDT
            var dept = rule != null
                ? departments.FirstOrDefault(d => d.DepartmentId == rule.DepartmentId)
                : departments.FirstOrDefault(d => d.DepartmentCode == "QLDT");
            if (dept == null) dept = departments.First();

            // Find manager for this department
            var manager = _db.DepartmentMembers
                .FirstOrDefault(dm => dm.DepartmentId == dept.DepartmentId && dm.IsManager && dm.IsActive);

            var assignedAt = issue.ReportedAt.AddMinutes(random.Next(5, 120));

            var assignment = new IssueAssignment
            {
                IssueId = issue.IssueId,
                DepartmentId = dept.DepartmentId,
                RoutingRuleId = rule?.RoutingRuleId,
                AssignedAt = assignedAt,
                AssignmentMethod = "AUTO",
                AssignmentNote = $"Phân công tự động theo quy tắc định tuyến: {rootType?.TypeName ?? "N/A"} → {dept.DepartmentName}",
                IsCurrent = true
            };

            if (issue.Status?.StatusCode != "NEW")
            {
                assignment.AcceptedAt = assignedAt.AddMinutes(random.Next(15, 180));
            }
            if (issue.Status?.StatusCode == "RESOLVED" ||
                issue.Status?.StatusCode == "CLOSED" ||
                issue.Status?.StatusCode == "REQUEST_REOPEN")
            {
                assignment.EndedAt = issue.ResolvedAt ?? assignedAt.AddDays(random.Next(1, 7));
            }

            _db.IssueAssignments.Add(assignment);
            await _db.SaveChangesAsync();
            created++;

            // IssueAssignmentMembers for non-NEW issues
            if (issue.Status?.StatusCode != "NEW")
            {
                var deptStaff = staffMembers.Where(s => s.DepartmentId == dept.DepartmentId).ToList();
                if (deptStaff.Count > 0)
                {
                    var staff = deptStaff[random.Next(deptStaff.Count)];
                    var status = issue.Status?.StatusCode switch
                    {
                        "RESOLVED" or "CLOSED" => AssignmentMemberStatus.Completed,
                        "IN_PROGRESS" or "REQUEST_REOPEN" => AssignmentMemberStatus.Accepted,
                        _ => AssignmentMemberStatus.Accepted
                    };

                    _db.IssueAssignmentMembers.Add(new IssueAssignmentMember
                    {
                        AssignmentId = assignment.AssignmentId,
                        UserId = staff.UserId,
                        AssignedBy = manager?.UserId ?? staff.UserId,
                        AssignedAt = assignment.AcceptedAt ?? assignedAt,
                        AcceptedAt = assignment.AcceptedAt,
                        EndedAt = assignment.EndedAt,
                        Status = status
                    });
                    await _db.SaveChangesAsync();
                }
            }
        }

        _logger.LogInformation("Seeded {Count} issue assignments.", created);
    }
}
