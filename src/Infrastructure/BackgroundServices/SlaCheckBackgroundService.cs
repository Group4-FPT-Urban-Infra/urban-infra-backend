using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.BackgroundServices;

/// <summary>
/// BackgroundService kiểm tra định kỳ các Issue_SLAs chưa hoàn thành.
/// Xử lý escalation dựa trên EscalationType:
/// - FirstResponseOverdue: Quá hạn phản hồi đầu tiên
/// - ApproachResponseDeadline: Sắp đến hạn phản hồi
/// - ResponseOverdue: Quá hạn giải quyết
/// 
/// Với mỗi loại, lấy tất cả rules phù hợp, kiểm tra escalation events đã tạo,
/// và gửi notification/updateAt khi đến thời gian.
/// </summary>
public class SlaCheckBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaCheckBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    private static readonly string[] TERMINAL_STATUS_CODES = { "RESOLVED", "CLOSED", "REJECTED" };
    private const string SYSTEM_USER = "SYSTEM";
    private const int REMINDER_INTERVAL_DAYS = 1;

    public SlaCheckBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SlaCheckBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SlaCheckBackgroundService] SLA Monitor Background Job starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndEscalateSlasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SlaCheckBackgroundService] An error occurred while checking Issue SLAs.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("[SlaCheckBackgroundService] SLA Monitor Background Job stopping.");
    }

    private async Task CheckAndEscalateSlasAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Lấy tất cả SLA chưa resolved và issue không ở trạng thái terminal
        var activeSlas = await context.IssueSlas
            .Include(s => s.Issue)
                .ThenInclude(i => i.Status)
            .Include(s => s.Issue)
                .ThenInclude(i => i.Priority)
            .Include(s => s.SlaPolicy)
            .Where(s => s.ResolvedAt == null)
            .Where(s => !TERMINAL_STATUS_CODES.Contains(s.Issue.Status.StatusCode))
            .ToListAsync(stoppingToken);

        if (activeSlas.Count == 0)
        {
            _logger.LogInformation("[SlaCheckBackgroundService] No active uncompleted Issue SLAs found at {Now}.", now);
            return;
        }

        _logger.LogInformation("[SlaCheckBackgroundService] Checking {Count} active Issue SLAs at {Now}.", activeSlas.Count, now);

        int escalationCreated = 0;
        int reminderSent = 0;
        int skipped = 0;

        foreach (var sla in activeSlas)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var statusCode = sla.Issue?.Status?.StatusCode ?? "UNKNOWN";
            var isNewStatus = string.Equals(statusCode, "NEW", StringComparison.OrdinalIgnoreCase);

            // Xác định các escalation types cần kiểm tra
            var escalationTypesToCheck = new List<EscalationType>();

            if (isNewStatus)
            {
                // Trạng thái NEW: kiểm tra FirstResponseOverdue và ApproachResponseDeadline
                escalationTypesToCheck.Add(EscalationType.FirstResponseOverdue);
                escalationTypesToCheck.Add(EscalationType.ApproachResponseDeadline);
            }
            else
            {
                // Đã phản hồi: kiểm tra ResponseOverdue
                escalationTypesToCheck.Add(EscalationType.ResponseOverdue);
            }

            foreach (var escalationType in escalationTypesToCheck)
            {
                var result = await ProcessEscalationByTypeAsync(context, sla, escalationType, now, stoppingToken);
                escalationCreated += result.Created;
                reminderSent += result.Reminder;
                skipped += result.Skipped;
            }
        }

        _logger.LogInformation(
            "[SlaCheckBackgroundService] SLA Check completed at {Now}: Escalations Created = {Created}, Reminders Sent = {Reminder}, Skipped = {Skipped}",
            now, escalationCreated, reminderSent, skipped);
    }

    /// <summary>
    /// Xử lý escalation theo EscalationType cụ thể.
    /// Lấy tất cả rules phù hợp với EscalationType, kiểm tra events đã tạo và xử lý.
    /// </summary>
    private async Task<EscalationResult> ProcessEscalationByTypeAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationType escalationType,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var result = new EscalationResult();

        // Xác định thời gian quá hạn dựa trên EscalationType
        int overdueMinutes;
        switch (escalationType)
        {
            case EscalationType.FirstResponseOverdue:
                overdueMinutes = GetFirstResponseOverdueMinutes(sla, now);
                break;
            case EscalationType.ApproachResponseDeadline:
                overdueMinutes = GetApproachDeadlineMinutes(sla, now);
                break;
            case EscalationType.ResponseOverdue:
                overdueMinutes = GetResponseOverdueMinutes(sla, now);
                break;
            default:
                return result;
        }

        // Nếu không có gì để xử lý
        if (overdueMinutes == int.MinValue)
        {
            return result;
        }

        // Lấy tất cả rules phù hợp với SLA policy và EscalationType
        var rules = await context.EscalationRules
            .AsNoTracking()
            .Where(r => r.SlaPolicyId == sla.SlaPolicyId 
                     && r.IsActive 
                     && r.EscalationType == escalationType)
            .OrderBy(r => r.EscalationLevel)
            .ToListAsync(stoppingToken);

        if (rules.Count == 0)
        {
            return result;
        }

        foreach (var rule in rules)
        {
            // Kiểm tra xem rule có trigger không
            // OverdueMinutes dương: trigger khi >= ngưỡng
            // OverdueMinutes âm: trigger khi <= ngưỡng (đối với APPROACH - sắp đến hạn)
            bool shouldTrigger;
            if (escalationType == EscalationType.ApproachResponseDeadline)
            {
                // OverdueMinutes âm, ví dụ -30 nghĩa là "30 phút trước deadline"
                shouldTrigger = overdueMinutes <= rule.OverdueMinutes && overdueMinutes >= 0;
            }
            else
            {
                // OverdueMinutes dương: trigger khi đã quá hạn
                shouldTrigger = overdueMinutes >= rule.OverdueMinutes;
            }

            if (!shouldTrigger)
            {
                continue;
            }

            var processResult = await ProcessEscalationRuleAsync(context, sla, rule, overdueMinutes, escalationType, now, stoppingToken);
            if (processResult.IsNew)
            {
                result.Created++;
            }
            else if (processResult.IsReminder)
            {
                result.Reminder++;
            }
            else
            {
                result.Skipped++;
            }
        }

        // Đánh dấu breach nếu cần
        await MarkBreachIfNeededAsync(context, sla, escalationType, overdueMinutes, stoppingToken);

        return result;
    }

    /// <summary>
    /// Lấy số phút quá hạn phản hồi đầu tiên. Trả về int.MinValue nếu không cần xử lý.
    /// </summary>
    private int GetFirstResponseOverdueMinutes(IssueSla sla, DateTime now)
    {
        if (sla.FirstRespondedAt != null)
        {
            return int.MinValue; // Đã phản hồi, không cần xử lý
        }

        if (!sla.FirstResponseDueAt.HasValue)
        {
            return int.MinValue; // Không có deadline
        }

        var deadline = sla.FirstResponseDueAt.Value;
        if (now <= deadline)
        {
            return int.MinValue; // Chưa quá hạn
        }

        return (int)Math.Floor((now - deadline).TotalMinutes);
    }

    /// <summary>
    /// Lấy số phút còn lại đến deadline phản hồi (giá trị dương nếu chưa đến deadline).
    /// Trả về int.MinValue nếu không cần xử lý.
    /// </summary>
    private int GetApproachDeadlineMinutes(IssueSla sla, DateTime now)
    {
        if (sla.FirstRespondedAt != null)
        {
            return int.MinValue; // Đã phản hồi
        }

        if (!sla.FirstResponseDueAt.HasValue)
        {
            return int.MinValue;
        }

        var deadline = sla.FirstResponseDueAt.Value;
        var minutesUntilDeadline = (int)Math.Floor((deadline - now).TotalMinutes);

        // Chỉ xử lý nếu còn dưới 0 phút (đã quá hạn) hoặc gần đến deadline
        // Để APPROACH xử lý cả khi quá hạn và khi sắp đến hạn
        return minutesUntilDeadline;
    }

    /// <summary>
    /// Lấy số phút quá hạn giải quyết. Trả về int.MinValue nếu không cần xử lý.
    /// </summary>
    private int GetResponseOverdueMinutes(IssueSla sla, DateTime now)
    {
        if (sla.ResolutionDueAt == DateTime.MinValue)
        {
            return int.MinValue;
        }

        var deadline = sla.ResolutionDueAt;
        if (now <= deadline)
        {
            return int.MinValue; // Chưa quá hạn
        }

        return (int)Math.Floor((now - deadline).TotalMinutes);
    }

    /// <summary>
    /// Đánh dấu breach nếu cần thiết.
    /// </summary>
    private async Task MarkBreachIfNeededAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationType escalationType,
        int overdueMinutes,
        CancellationToken stoppingToken)
    {
        if (overdueMinutes <= 0)
        {
            return;
        }

        bool shouldMark = false;

        switch (escalationType)
        {
            case EscalationType.FirstResponseOverdue:
                if (!sla.IsFirstResponseBreached)
                {
                    sla.IsFirstResponseBreached = true;
                    shouldMark = true;
                }
                break;
            case EscalationType.ResponseOverdue:
                if (!sla.IsResolutionBreached)
                {
                    sla.IsResolutionBreached = true;
                    shouldMark = true;
                }
                break;
        }

        if (shouldMark)
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Xử lý một escalation rule cụ thể.
    /// Kiểm tra xem đã có EscalationEvent chưa, nếu có thì kiểm tra thời gian để gửi reminder.
    /// </summary>
    private async Task<EscalationProcessResult> ProcessEscalationRuleAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationRule rule,
        int overdueMinutes,
        EscalationType escalationType,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var result = new EscalationProcessResult();

        // Kiểm tra xem đã có EscalationEvent cho rule này chưa
        var existingEvent = await context.EscalationEvents
            .Where(e => e.IssueId == sla.IssueId
                     && e.EscalationRuleId == rule.Id
                     && !e.IsDeleted)
            .OrderByDescending(e => e.TriggeredAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (existingEvent != null)
        {
            // Đã có event, kiểm tra thời gian để gửi reminder
            var lastUpdate = existingEvent.UpdatedAtUtc ?? existingEvent.TriggeredAt;
            var daysSinceLastUpdate = (now - lastUpdate).TotalDays;

            if (daysSinceLastUpdate >= REMINDER_INTERVAL_DAYS)
            {
                // Đã quá 1 ngày, gửi reminder và update UpdatedAt
                await SendEscalationReminderAsync(context, sla, existingEvent, rule, overdueMinutes, escalationType, now, stoppingToken);
                result.IsReminder = true;
            }
            else
            {
                result.IsSkipped = true;
            }
        }
        else
        {
            // Chưa có event, tạo mới
            await CreateEscalationEventAsync(context, sla, rule, overdueMinutes, escalationType, now, stoppingToken);
            result.IsNew = true;
        }

        return result;
    }

    /// <summary>
    /// Tạo mới EscalationEvent và các notification tương ứng.
    /// </summary>
    private async Task CreateEscalationEventAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationRule rule,
        int overdueMinutes,
        EscalationType escalationType,
        DateTime now,
        CancellationToken stoppingToken)
    {
        // Lấy thông tin issue
        var issue = await context.Issues
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IssueId == sla.IssueId, stoppingToken);

        if (issue == null) return;

        var publicCode = issue.PublicCode;

        // Lấy department ID của issue
        var issueDepartmentId = await GetIssueDepartmentIdAsync(context, sla.IssueId, stoppingToken);

        // Xác định TargetDepartmentId cho EscalationEvent dựa trên TargetRoleName
        // - Admin: TargetDepartmentId = null
        // - DepartmentManager có TargetDepartmentId: dùng TargetDepartmentId của rule
        // - DepartmentManager không có TargetDepartmentId: dùng department của issue
        // - DepartmentStaff: luôn dùng department của issue
        int? eventDepartmentId;
        if (rule.TargetRoleName == Roles.Admin)
        {
            eventDepartmentId = null;
        }
        else if (rule.TargetRoleName == Roles.DepartmentManager)
        {
            eventDepartmentId = rule.TargetDepartmentId ?? issueDepartmentId;
        }
        else // DepartmentStaff
        {
            eventDepartmentId = issueDepartmentId;
        }

        // Xây dựng message dựa trên escalation type
        var escalationTypeText = GetEscalationTypeText(escalationType, overdueMinutes);

        var note = $"[ESCALATION {escalationType}] Mức {rule.EscalationLevel}: Sự cố {publicCode} {escalationTypeText}. "
                  + $"Mức ưu tiên: {sla.Issue?.Priority?.PriorityName ?? "N/A"}. "
                  + $"Đối tượng: {rule.TargetRoleName ?? "N/A"}. "
                  + $"{(eventDepartmentId.HasValue ? $"Phòng ban: {eventDepartmentId}" : "Tất cả phòng ban")}";

        // Tạo EscalationEvent với TargetDepartmentId đã được resolve
        var escalationEvent = new EscalationEvent
        {
            IssueId = sla.IssueId,
            EscalationRuleId = rule.Id,
            TargetDepartmentId = eventDepartmentId,
            TargetUserId = null,
            TriggeredAt = now,
            EventStatus = escalationType == EscalationType.ApproachResponseDeadline ? "Warning" : "Pending",
            Note = note,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.EscalationEvents.Add(escalationEvent);
        await context.SaveChangesAsync(stoppingToken);

        _logger.LogWarning(
            "[SLA ESCALATION] Issue {IssueId} ({PublicCode}) - Type: {EscalationType}, Level: {Level}, OverdueMinutes: {OverdueMinutes}, Role: {Role}, Dept: {Dept}",
            sla.IssueId, publicCode, escalationType, rule.EscalationLevel, overdueMinutes,
            rule.TargetRoleName ?? "N/A", eventDepartmentId?.ToString() ?? "All");

        // Tạo IssueUpdate nếu không phải là warning (approaching)
        if (escalationType != EscalationType.ApproachResponseDeadline)
        {
            await CreateIssueUpdateAsync(context, sla.IssueId, rule, note, now, stoppingToken);
        }

        // Tạo notifications
        await CreateNotificationsAsync(context, sla, rule, escalationEvent, publicCode, escalationType, overdueMinutes, now, stoppingToken);
    }

    /// <summary>
    /// Gửi reminder cho EscalationEvent đã tồn tại và cập nhật UpdatedAt.
    /// </summary>
    private async Task SendEscalationReminderAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationEvent existingEvent,
        EscalationRule rule,
        int overdueMinutes,
        EscalationType escalationType,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var issue = await context.Issues
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IssueId == sla.IssueId, stoppingToken);

        if (issue == null) return;

        var publicCode = issue.PublicCode;

        _logger.LogWarning(
            "[SLA REMINDER] Issue {IssueId} ({PublicCode}) - Type: {EscalationType}, Level: {Level}, Reminder after 1 day, OverdueMinutes: {OverdueMinutes}",
            sla.IssueId, publicCode, escalationType, rule.EscalationLevel, overdueMinutes);

        // Cập nhật UpdatedAtUtc của EscalationEvent
        existingEvent.UpdatedAtUtc = now;
        existingEvent.EventStatus = "ReminderSent";
        await context.SaveChangesAsync(stoppingToken);

        // Tạo reminder notifications
        await CreateReminderNotificationsAsync(context, sla, rule, existingEvent, publicCode, escalationType, overdueMinutes, now, stoppingToken);
    }

    /// <summary>
    /// Tạo IssueUpdate ghi nhận escalation event.
    /// </summary>
    private async Task CreateIssueUpdateAsync(
        AppDbContext context,
        long issueId,
        EscalationRule rule,
        string note,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var issue = await context.Issues
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IssueId == issueId, stoppingToken);

        if (issue == null) return;

        var issueUpdate = new IssueUpdate
        {
            IssueId = issueId,
            CreatedBy = SYSTEM_USER,
            FromStatusId = issue.StatusId,
            ToStatusId = issue.StatusId,
            Note = note,
            ProgressPercent = null,
            IsSystemGenerated = true,
            CreatedAt = now
        };

        context.IssueUpdates.Add(issueUpdate);
        await context.SaveChangesAsync(stoppingToken);
    }

    /// <summary>
    /// Tạo notifications cho escalation event mới.
    /// </summary>
    private async Task CreateNotificationsAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationRule rule,
        EscalationEvent escalationEvent,
        string publicCode,
        EscalationType escalationType,
        int overdueMinutes,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var targetUserIds = new HashSet<string>();

        // Thêm reporter
        var reporterId = await context.Issues
            .Where(i => i.IssueId == sla.IssueId)
            .Select(i => i.ReporterId)
            .FirstOrDefaultAsync(stoppingToken);
        if (!string.IsNullOrWhiteSpace(reporterId))
        {
            targetUserIds.Add(reporterId);
        }

        // Lấy department ID của issue hiện tại từ IssueAssignment
        var issueDepartmentId = await GetIssueDepartmentIdAsync(context, sla.IssueId, stoppingToken);

        // Resolve target users dựa trên TargetRoleName và TargetDepartmentId
        var resolvedUserIds = await ResolveTargetUsersAsync(context, rule, issueDepartmentId, stoppingToken);
        foreach (var userId in resolvedUserIds)
        {
            targetUserIds.Add(userId);
        }

        // Xây dựng tiêu đề và nội dung notification
        var (title, message) = BuildNotificationContent(publicCode, escalationType, rule.EscalationLevel, overdueMinutes);

        // Override với custom template nếu có
        if (!string.IsNullOrWhiteSpace(rule.NotificationTitle))
        {
            title = rule.NotificationTitle
                .Replace("{IssueCode}", publicCode)
                .Replace("{Level}", rule.EscalationLevel.ToString());
        }
        if (!string.IsNullOrWhiteSpace(rule.NotificationTemplate))
        {
            message = rule.NotificationTemplate
                .Replace("{IssueCode}", publicCode)
                .Replace("{Level}", rule.EscalationLevel.ToString())
                .Replace("{OverdueMinutes}", overdueMinutes.ToString());
        }

        // Tạo notifications
        foreach (var userId in targetUserIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = "ESCALATION",
                IssueId = sla.IssueId,
                IsRead = false,
                CreatedAt = now
            };
            context.Notifications.Add(notification);
        }

        if (targetUserIds.Count > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Resolve users dựa trên TargetRoleName và TargetDepartmentId.
    /// - Admin: gửi đến tất cả Admin users
    /// - DepartmentManager: gửi đến manager của TargetDepartmentId (hoặc department hiện tại của issue nếu TargetDepartmentId = null)
    /// </summary>
    private async Task<List<string>> ResolveTargetUsersAsync(
        AppDbContext context,
        EscalationRule rule,
        int? issueDepartmentId,
        CancellationToken stoppingToken)
    {
        var userIds = new List<string>();

        if (string.IsNullOrWhiteSpace(rule.TargetRoleName))
        {
            return userIds;
        }

        switch (rule.TargetRoleName)
        {
            case Roles.Admin:
                // Lấy tất cả users có role Admin
                var adminUsers = await context.UserRoles
                    .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == Roles.Admin))
                    .Select(ur => ur.UserId)
                    .ToListAsync(stoppingToken);
                userIds.AddRange(adminUsers);
                break;

            case Roles.DepartmentManager:
                // Lấy department ID để query
                var departmentId = rule.TargetDepartmentId ?? issueDepartmentId;
                if (departmentId.HasValue)
                {
                    // Lấy tất cả user IDs có role DepartmentManager
                    var managerUserIds = await context.UserRoles
                        .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == Roles.DepartmentManager))
                        .Select(ur => ur.UserId)
                        .ToListAsync(stoppingToken);

                    // Lọc theo department của user
                    var deptManagers = await context.DepartmentMembers
                        .Where(dm => dm.DepartmentId == departmentId.Value 
                                 && dm.IsActive 
                                 && managerUserIds.Contains(dm.UserId))
                        .Select(dm => dm.UserId)
                        .ToListAsync(stoppingToken);
                    userIds.AddRange(deptManagers);
                }
                break;

            case Roles.DepartmentStaff:
                // Lấy department ID để query
                var staffDeptId = rule.TargetDepartmentId ?? issueDepartmentId;
                if (staffDeptId.HasValue)
                {
                    var staffUsers = await context.DepartmentMembers
                        .Where(dm => dm.DepartmentId == staffDeptId.Value && dm.IsActive)
                        .Select(dm => dm.UserId)
                        .ToListAsync(stoppingToken);
                    userIds.AddRange(staffUsers);
                }
                break;

            default:
                // Nếu không match, thử lấy users theo role
                var roleUsers = await context.UserRoles
                    .Where(ur => context.Roles.Any(r => r.Id == ur.RoleId && r.Name == rule.TargetRoleName))
                    .Select(ur => ur.UserId)
                    .ToListAsync(stoppingToken);
                userIds.AddRange(roleUsers);
                break;
        }

        return userIds.Distinct().ToList();
    }

    /// <summary>
    /// Lấy department ID hiện tại của issue từ IssueAssignment.
    /// </summary>
    private async Task<int?> GetIssueDepartmentIdAsync(
        AppDbContext context,
        long issueId,
        CancellationToken stoppingToken)
    {
        var departmentId = await context.IssueAssignments
            .Where(a => a.IssueId == issueId && a.IsCurrent)
            .Select(a => (int?)a.DepartmentId)
            .FirstOrDefaultAsync(stoppingToken);

        return departmentId;
    }

    /// <summary>
    /// Tạo reminder notifications.
    /// </summary>
    private async Task CreateReminderNotificationsAsync(
        AppDbContext context,
        IssueSla sla,
        EscalationRule rule,
        EscalationEvent escalationEvent,
        string publicCode,
        EscalationType escalationType,
        int overdueMinutes,
        DateTime now,
        CancellationToken stoppingToken)
    {
        var targetUserIds = new HashSet<string>();

        // Thêm reporter
        var reporterId = await context.Issues
            .Where(i => i.IssueId == sla.IssueId)
            .Select(i => i.ReporterId)
            .FirstOrDefaultAsync(stoppingToken);
        if (!string.IsNullOrWhiteSpace(reporterId))
        {
            targetUserIds.Add(reporterId);
        }

        // Lấy department ID của issue từ IssueAssignment
        var issueDepartmentId = await GetIssueDepartmentIdAsync(context, sla.IssueId, stoppingToken);

        // Resolve target users
        var resolvedUserIds = await ResolveTargetUsersAsync(context, rule, issueDepartmentId, stoppingToken);
        foreach (var userId in resolvedUserIds)
        {
            targetUserIds.Add(userId);
        }

        // Xây dựng tiêu đề reminder
        var escalationTypeText = GetEscalationTypeText(escalationType, overdueMinutes);
        var title = $"[NHẮC NHỞ] Sự cố {publicCode}";
        var message = $"Sự cố {publicCode} đã được escalation lần 2 do vẫn chưa được xử lý sau 1 ngày. "
                    + $"{escalationTypeText}. Mức {rule.EscalationLevel}.";

        foreach (var userId in targetUserIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = "ESCALATION_REMINDER",
                IssueId = sla.IssueId,
                IsRead = false,
                CreatedAt = now
            };
            context.Notifications.Add(notification);
        }

        if (targetUserIds.Count > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Xây dựng text mô tả escalation type.
    /// </summary>
    private static string GetEscalationTypeText(EscalationType escalationType, int overdueMinutes)
    {
        return escalationType switch
        {
            EscalationType.FirstResponseOverdue => $"quá hạn phản hồi đầu tiên {overdueMinutes} phút",
            EscalationType.ApproachResponseDeadline => $"sắp đến hạn phản hồi (còn {Math.Abs(overdueMinutes)} phút hoặc quá hạn)",
            EscalationType.ResponseOverdue => $"quá hạn giải quyết {overdueMinutes} phút",
            _ => "cảnh báo SLA"
        };
    }

    /// <summary>
    /// Xây dựng nội dung notification dựa trên escalation type.
    /// </summary>
    private static (string Title, string Message) BuildNotificationContent(
        string publicCode,
        EscalationType escalationType,
        int level,
        int overdueMinutes)
    {
        return escalationType switch
        {
            EscalationType.FirstResponseOverdue => (
                $"[CẢNH BÁO] Sự cố {publicCode} quá hạn phản hồi",
                $"Sự cố {publicCode} đã quá hạn phản hồi đầu tiên {overdueMinutes} phút. "
                + $"Vui lòng phản hồi công dân ngay. (Escalation Mức {level})"
            ),
            EscalationType.ApproachResponseDeadline => (
                $"[CẢNH BÁO SẮP HẠN] Sự cố {publicCode}",
                $"Sự cố {publicCode} sắp đến hạn phản hồi (còn {Math.Abs(overdueMinutes)} phút). "
                + $"Vui lòng ưu tiên xử lý. (Escalation Mức {level})"
            ),
            EscalationType.ResponseOverdue => (
                $"[NGHIÊM TRỌNG] Sự cố {publicCode} quá hạn giải quyết",
                $"Sự cố {publicCode} đã quá hạn giải quyết {overdueMinutes} phút. "
                + $"Vui lòng xử lý ngay. (Escalation Mức {level})"
            ),
            _ => (
                $"[ESCALATION] Sự cố {publicCode}",
                $"Sự cố {publicCode} đã kích hoạt escalation mức {level}."
            )
        };
    }

    /// <summary>
    /// Kết quả xử lý escalation cho một SLA.
    /// </summary>
    private class EscalationResult
    {
        public int Created { get; set; }
        public int Reminder { get; set; }
        public int Skipped { get; set; }
    }

    /// <summary>
    /// Kết quả xử lý một escalation rule.
    /// </summary>
    private class EscalationProcessResult
    {
        public bool IsNew { get; set; }
        public bool IsReminder { get; set; }
        public bool IsSkipped { get; set; }
    }
}
