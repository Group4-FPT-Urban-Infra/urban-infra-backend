using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
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

        var rules = new List<EscalationRule>();

        foreach (var sla in slaPolicies)
        {
            // ============================================================
            // Tìm phòng ban mặc định cho SLA này (từ RoutingRules)
            // ============================================================
            var routingRule = _db.RoutingRules
                .FirstOrDefault(r => r.IssueTypeId == sla.IssueTypeId);
            int? currentDeptId = routingRule?.DepartmentId;

            // Tính parent và grandparent department
            int? parentDeptId = null;
            int? grandparentDeptId = null;

            if (currentDeptId != null)
            {
                var currentDept = departments.FirstOrDefault(d => d.DepartmentId == currentDeptId);
                if (currentDept != null)
                {
                    if (currentDept.ParentDepartmentId != null)
                    {
                        parentDeptId = currentDept.ParentDepartmentId;
                        var parentDept = departments.FirstOrDefault(d => d.DepartmentId == parentDeptId);
                        if (parentDept?.ParentDepartmentId != null)
                        {
                            grandparentDeptId = parentDept.ParentDepartmentId;
                        }
                    }
                }
            }

            // ============================================================
            // 1. FIRST_RESPONSE_OVERDUE - Quá hạn phản hồi đầu tiên
            // Level 1: DepartmentManager (OverdueMinutes = 0)
            // Issue mới tạo chưa có staff được assign, nên gửi đến manager
            // TargetDepartmentId = null => dùng department của issue khi gửi
            // ============================================================
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = 0, // Trigger ngay khi vừa hết hạn
                EscalationType = EscalationType.FirstResponseOverdue,
                EscalationLevel = 1,
                TargetRoleName = Roles.DepartmentManager,
                TargetDepartmentId = null, // Sẽ dùng department của issue khi gửi notification
                NotificationTitle = "[CẢNH BÁO] Sự cố chưa được phản hồi",
                NotificationTemplate = "Sự cố [{IssueCode}] đã hết hạn phản hồi đầu tiên. Vui lòng kiểm tra và phản hồi ngay.",
                IsActive = true
            });

            // ============================================================
            // 2. APPROACH_RESPONSE_DEADLINE - Sắp đến hạn phản hồi
            // Level 1: DepartmentStaff (OverdueMinutes = -WarningBeforeMinutes)
            // Cảnh báo trước khi hết hạn
            // ============================================================
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = -sla.WarningBeforeMinutes, // Âm = X phút trước deadline
                EscalationType = EscalationType.ApproachResponseDeadline,
                EscalationLevel = 1,
                TargetRoleName = Roles.DepartmentStaff,
                TargetDepartmentId = null, // Sẽ dùng department của issue khi gửi notification
                NotificationTitle = "[NHẮC NHỞ] Sự cố sắp đến hạn phản hồi",
                NotificationTemplate = "Sự cố [{IssueCode}] sắp đến hạn phản hồi đầu tiên (còn {OverdueMinutes} phút). Vui lòng ưu tiên xử lý.",
                IsActive = true
            });

            // ============================================================
            // 3. RESPONSE_OVERDUE - Quá hạn giải quyết
            // Level 1: DepartmentStaff (OverdueMinutes = 0)
            // ============================================================
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = 0, // Trigger ngay khi vừa hết hạn
                EscalationType = EscalationType.ResponseOverdue,
                EscalationLevel = 1,
                TargetRoleName = Roles.DepartmentStaff,
                TargetDepartmentId = null, // Sẽ dùng department của issue khi gửi notification
                NotificationTitle = "[NGHIÊM TRỌNG] Sự cố quá hạn giải quyết",
                NotificationTemplate = "Sự cố [{IssueCode}] đã hết hạn giải quyết. Vui lòng xử lý ngay.",
                IsActive = true
            });

            // ============================================================
            // 4. RESPONSE_OVERDUE - Level 2
            // DepartmentManager (OverdueMinutes = 0, TargetDepartmentId = null)
            // Gửi đến manager của issue's department
            // ============================================================
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = 1,
                EscalationType = EscalationType.ResponseOverdue,
                EscalationLevel = 2,
                TargetRoleName = Roles.DepartmentManager,
                TargetDepartmentId = null, // Sẽ dùng department của issue
                NotificationTitle = "[NGHIÊM TRỌNG] Sự cố quá hạn - Cần can thiệp",
                NotificationTemplate = "Sự cố [{IssueCode}] đã hết hạn giải quyết. Yêu cầu manager can thiệp và báo cáo.",
                IsActive = true
            });

            // ============================================================
            // 5. RESPONSE_OVERDUE - Level 3
            // DepartmentManager (OverdueMinutes = 0, TargetDepartmentId = parentDeptId)
            // Gửi đến manager của parent department
            // ============================================================
            if (parentDeptId.HasValue)
            {
                rules.Add(new EscalationRule
                {
                    SlaPolicyId = sla.Id,
                    OverdueMinutes = 2,
                    EscalationType = EscalationType.ResponseOverdue,
                    EscalationLevel = 3,
                    TargetRoleName = Roles.DepartmentManager,
                    TargetDepartmentId = parentDeptId, // Gửi đến manager của parent department
                    NotificationTitle = "[KHẨN] Sự cố quá hạn - Escalate cấp 3",
                    NotificationTemplate = "Sự cố [{IssueCode}] đã hết hạn giải quyết và được escalation lên cấp 3. Cần báo cáo cấp trên.",
                    IsActive = true
                });
            }

            // ============================================================
            // 6. RESPONSE_OVERDUE - Level 4
            // DepartmentManager (OverdueMinutes = 0, TargetDepartmentId = grandparentDeptId)
            // Gửi đến manager của grandparent department
            // ============================================================
            if (grandparentDeptId.HasValue)
            {
                rules.Add(new EscalationRule
                {
                    SlaPolicyId = sla.Id,
                    OverdueMinutes = 3,
                    EscalationType = EscalationType.ResponseOverdue,
                    EscalationLevel = 4,
                    TargetRoleName = Roles.DepartmentManager,
                    TargetDepartmentId = grandparentDeptId, // Gửi đến manager của grandparent department
                    NotificationTitle = "[KHẨN CẤP] Sự cố quá hạn - Escalate cấp 4",
                    NotificationTemplate = "Sự cố [{IssueCode}] đã hết hạn giải quyết và được escalation lên cấp cao nhất. Yêu cầu can thiệp khẩn.",
                    IsActive = true
                });
            }

            // ============================================================
            // 7. RESPONSE_OVERDUE - Level 5 (Admin)
            // Admin (OverdueMinutes = 0, TargetDepartmentId = null)
            // Gửi đến tất cả Admin users
            // ============================================================
            var adminLevel = grandparentDeptId.HasValue ? 5 : (parentDeptId.HasValue ? 4 : 3);
            rules.Add(new EscalationRule
            {
                SlaPolicyId = sla.Id,
                OverdueMinutes = 4,
                EscalationType = EscalationType.ResponseOverdue,
                EscalationLevel = adminLevel,
                TargetRoleName = Roles.Admin,
                TargetDepartmentId = null, // Admin không cần department cụ thể
                NotificationTitle = "[Admin] Sự cố QUÁ HẠN giải quyết",
                NotificationTemplate = "Sự cố [{IssueCode}] đã hết hạn giải quyết. Admin vui lòng kiểm tra và can thiệp.",
                IsActive = true
            });
        }

        _db.EscalationRules.AddRange(rules);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} escalation rules.", rules.Count);
    }
}
