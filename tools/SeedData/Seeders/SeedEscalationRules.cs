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
        if (slaPolicies.Count == 0)
        {
            _logger.LogWarning("No SLA policies found. Run SeedSlaPolicies first.");
            return;
        }

        var departments = _db.Departments.ToList();
        var priorities = _db.IssuePriorities.ToList();

        var rules = new List<EscalationRule>();

        foreach (var sla in slaPolicies)
        {
            var priority = priorities.FirstOrDefault(p => p.PriorityId == sla.PriorityId);
            var isHighPriority = priority != null &&
                (priority.PriorityCode == "CRITICAL" || priority.PriorityCode == "HIGH");

            // ============================================================
            // Level 1: No response warning
            // OverdueMinutes = WarningBeforeMinutes → warn DepartmentManager of current dept
            // ============================================================
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = sla.WarningBeforeMinutes,
                EscalationLevel = 1,
                TargetRoleName = "DepartmentManager",
                NotificationTitle = "Cảnh báo: Sự cố chưa được phản hồi",
                NotificationTemplate = "Sự cố [{IssueCode}] đã đến thời hạn phản hồi đầu tiên ({FirstResponseDueAt}). " +
                    "Vui lòng kiểm tra và xử lý ngay.",
                IsActive = true
            });

            // ============================================================
            // Level 2: Resolution risk
            // OverdueMinutes = ResolutionMinutes * 0.75 → escalate to parent department
            // ============================================================
            // Find a representative department for this SLA (from RoutingRules)
            var routingRule = _db.RoutingRules
                .FirstOrDefault(r => r.IssueTypeId == sla.IssueTypeId);
            int? parentDeptId = null;

            if (routingRule != null)
            {
                var currentDept = departments.FirstOrDefault(d => d.DepartmentId == routingRule.DepartmentId);
                if (currentDept?.ParentDepartmentId != null)
                    parentDeptId = currentDept.ParentDepartmentId;
            }

            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = (int)(sla.ResolutionMinutes * 0.75),
                EscalationLevel = 2,
                TargetRoleName = "DepartmentManager",
                TargetDepartmentId = parentDeptId, // Escalate to parent department manager
                NotificationTitle = "Cảnh báo: Sự cố có nguy cơ trễ hạn giải quyết",
                NotificationTemplate = "Sự cố [{IssueCode}] sắp quá hạn giải quyết (deadline: {ResolutionDueAt}). " +
                    "Cần ưu tiên xử lý và báo cáo cấp trên.",
                IsActive = true
            });

            // ============================================================
            // Level 3: Overdue — different targets by priority
            // CRITICAL/HIGH → DepartmentManager of grandparent (if exists)
            // All priorities → Admin receives notification
            // ============================================================
            if (isHighPriority)
            {
                int? grandparentDeptId = null;
                if (parentDeptId != null)
                {
                    var parent = departments.FirstOrDefault(d => d.DepartmentId == parentDeptId);
                    if (parent?.ParentDepartmentId != null)
                        grandparentDeptId = parent.ParentDepartmentId;
                }

                rules.Add(new EscalationRule
                {
                    SlaPolicyId = sla.Id,
                    OverdueMinutes = sla.ResolutionMinutes,
                    EscalationLevel = 3,
                    TargetRoleName = "DepartmentManager",
                    TargetDepartmentId = grandparentDeptId ?? parentDeptId,
                    NotificationTitle = "[QUAN TRỌNG] Sự cố QUÁ HẠN giải quyết - Ưu tiên CAO",
                    NotificationTemplate = "Sự cố [{IssueCode}] đã QUÁ HẠN giải quyết. " +
                        "Yêu cầu báo cáo lãnh đạo cấp cao và đề xuất phương án xử lý khẩn cấp.",
                    IsActive = true
                });
            }

            // Admin always receives overdue notification
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = sla.ResolutionMinutes,
                EscalationLevel = isHighPriority ? 4 : 3,
                TargetRoleName = "Admin",
                NotificationTitle = $"[Admin] Sự cố QUÁ HẠN: [{priority?.PriorityCode ?? "?"}]",
                NotificationTemplate = "Sự cố [{IssueCode}] (ưu tiên: {Priority}) đã quá hạn giải quyết. " +
                    "Admin vui lòng kiểm tra và can thiệp.",
                IsActive = true
            });
        }

        _db.EscalationRules.AddRange(rules);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} escalation rules.", rules.Count);
    }
}
