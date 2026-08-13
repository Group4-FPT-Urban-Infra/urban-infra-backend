using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.SeedData.Seeders;

public class SeedEscalationRules
{
    private readonly AppDbContext _db;
    private readonly ILogger _logger;

    public SeedEscalationRules(AppDbContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (_db.EscalationRules.Any())
        {
            _logger.LogInformation("EscalationRules already exist, skipping.");
            return;
        }

        var slaPolicies = _db.SlaPolicies.ToList();
        var departments = _db.Departments.ToList();

        if (slaPolicies.Count == 0)
        {
            _logger.LogWarning("No SLA policies found. Run SeedSlaPolicies first.");
            return;
        }

        var rules = new List<EscalationRule>();

        foreach (var sla in slaPolicies)
        {
            // Level 1: Escalate to Department Manager if no response within 50% of first response time
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = (int)(sla.FirstResponseMinutes * 1.5), // 150% of first response = overdue
                EscalationLevel = 1,
                TargetRoleName = "DepartmentManager",
                NotificationTitle = "Sự cố chưa được phản hồi",
                NotificationTemplate = "Sự cố [{IssueCode}] đã quá hạn phản hồi đầu tiên ({FirstResponseDueAt}). Vui lòng kiểm tra và xử lý.",
                IsActive = true
            });

            // Level 2: Escalate to Admin if no resolution within 75% of resolution time
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = (int)(sla.ResolutionMinutes * 0.75), // 75% of resolution time
                EscalationLevel = 2,
                TargetRoleName = "Admin",
                NotificationTitle = "Sự cố có nguy cơ trễ hạn",
                NotificationTemplate = "Sự cố [{IssueCode}] sắp quá hạn giải quyết (due: {ResolutionDueAt}). Cần ưu tiên xử lý.",
                IsActive = true
            });

            // Level 3: Escalate to specific department head if completely overdue
            // Only for CRITICAL and HIGH priority issues
            var priority = _db.IssuePriorities.FirstOrDefault(p => p.PriorityId == sla.PriorityId);
            if (priority != null && (priority.PriorityCode == "CRITICAL" || priority.PriorityCode == "HIGH"))
            {
                var issueType = _db.IssueTypes.FirstOrDefault(t => t.IssueTypeId == sla.IssueTypeId);
                var dept = issueType != null ? departments.FirstOrDefault() : null; // First matching dept as fallback

                rules.Add(new EscalationRule
                {
                    SlaPolicyId = sla.Id,
                    OverdueMinutes = sla.ResolutionMinutes, // At resolution deadline
                    EscalationLevel = 3,
                    TargetRoleName = "DepartmentManager",
                    TargetDepartmentId = dept?.DepartmentId,
                    NotificationTitle = "Sự cố QUÁ HẠN giải quyết",
                    NotificationTemplate = "Sự cố [{IssueCode}] đã QUÁ HẠN giải quyết. Cần báo cáo lãnh đạo và đề xuất phương án.",
                    IsActive = true
                });
            }
        }

        _db.EscalationRules.AddRange(rules);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} escalation rules.", rules.Count);
    }
}
