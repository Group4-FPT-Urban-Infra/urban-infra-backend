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

        var issues = _db.Issues.ToList();
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

        var assignments = new List<IssueAssignment>();
        var random = new Random(42);

        // Map issue types to departments
        var typeToDeptMap = new Dictionary<string, string>
        {
            { "LIGHT", "QLDT" },
            { "POTHOLE", "CSGT" },
            { "ROAD", "CSGT" },
            { "SIGN", "GT" },
            { "FLOOD", "CSGT" },
            { "DRAIN", "CSGT" },
            { "TREE", "QLDT" },
            { "GARBAGE", "VSDN" }
        };

        // Also create staff members for assignment
        var staffMembers = _db.DepartmentMembers
            .Where(dm => !dm.IsManager && dm.IsActive)
            .ToList();

        var assignmentCount = 0;

        foreach (var issue in issues)
        {
            var issueType = _db.IssueTypes.FirstOrDefault(t => t.IssueTypeId == issue.IssueTypeId);
            if (issueType == null) continue;

            // Find matching department
            var deptCode = typeToDeptMap.GetValueOrDefault(issueType.TypeCode, departments[random.Next(departments.Count)].DepartmentCode);
            var dept = departments.FirstOrDefault(d => d.DepartmentCode == deptCode);
            if (dept == null) dept = departments.First();

            // Find manager for this department
            var manager = _db.DepartmentMembers
                .FirstOrDefault(dm => dm.DepartmentId == dept.DepartmentId && dm.IsManager && dm.IsActive);

            if (manager == null)
            {
                _logger.LogWarning("No manager found for department {Dept}, skipping issue {Issue}",
                    dept.DepartmentCode, issue.PublicCode);
                continue;
            }

            var assignment = new IssueAssignment
            {
                IssueId = issue.IssueId,
                DepartmentId = dept.DepartmentId,
                AssignedAt = issue.ReportedAt.AddMinutes(random.Next(5, 60)),
                AssignmentMethod = "AUTO",
                IsCurrent = true
            };

            assignments.Add(assignment);
            assignmentCount++;
        }

        if (assignments.Count > 0)
        {
            _db.IssueAssignments.AddRange(assignments);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} issue assignments.", assignments.Count);

            // Now seed IssueAssignmentMembers for some assignments
            // We'll mark ~40% as assigned to staff, leaving ~60% unassigned
            await SeedIssueAssignmentMembersAsync(assignments, staffMembers, random);
        }
        else
        {
            _logger.LogInformation("No assignments to seed.");
        }
    }

    private async Task SeedIssueAssignmentMembersAsync(
        List<IssueAssignment> assignments,
        List<DepartmentMember> staffMembers,
        Random random)
    {
        if (_db.IssueAssignmentMembers.Any())
        {
            _logger.LogInformation("IssueAssignmentMembers already exist, skipping.");
            return;
        }

        if (staffMembers.Count == 0)
        {
            _logger.LogWarning("No staff members found. Skipping assignment members seeding.");
            return;
        }

        var assignmentMembers = new List<IssueAssignmentMember>();
        var assignedCount = 0;

        // Assign ~40% of assignments to staff members
        foreach (var assignment in assignments)
        {
            // Only assign if random condition is met (about 40%)
            if (random.Next(100) >= 40) continue;

            // Get staff members from the same department
            var deptStaff = staffMembers
                .Where(s => s.DepartmentId == assignment.DepartmentId)
                .ToList();

            if (deptStaff.Count == 0) continue;

            // Pick 1 staff member for this assignment
            var staff = deptStaff[random.Next(deptStaff.Count)];

            // Find the manager who assigned this
            var manager = _db.DepartmentMembers
                .FirstOrDefault(dm => dm.DepartmentId == assignment.DepartmentId && dm.IsManager);

            // Determine status based on issue status
            var issue = _db.Issues.FirstOrDefault(i => i.IssueId == assignment.IssueId);

            string status;
            DateTime? acceptedAt = null;
            DateTime? endedAt = null;

            if (issue?.Status?.StatusCode == "RESOLVED" || issue?.Status?.StatusCode == "CLOSED")
            {
                status = AssignmentMemberStatus.Completed;
                acceptedAt = assignment.AssignedAt.AddMinutes(random.Next(10, 120));
                endedAt = acceptedAt.Value.AddHours(random.Next(1, 24));
            }
            else if (issue?.Status?.StatusCode == "IN_PROGRESS")
            {
                status = AssignmentMemberStatus.Accepted;
                acceptedAt = assignment.AssignedAt.AddMinutes(random.Next(10, 60));
            }
            else
            {
                status = AssignmentMemberStatus.Pending;
            }

            assignmentMembers.Add(new IssueAssignmentMember
            {
                AssignmentId = assignment.AssignmentId,
                UserId = staff.UserId,
                AssignedBy = manager?.UserId ?? staff.UserId,
                AssignedAt = assignment.AssignedAt,
                AcceptedAt = acceptedAt,
                EndedAt = endedAt,
                Status = status
            });

            assignedCount++;
        }

        if (assignmentMembers.Count > 0)
        {
            _db.IssueAssignmentMembers.AddRange(assignmentMembers);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} issue assignment members ({Assigned} assigned, {Unassigned} unassigned).",
                assignmentMembers.Count, assignedCount, assignments.Count - assignedCount);
        }
    }
}
