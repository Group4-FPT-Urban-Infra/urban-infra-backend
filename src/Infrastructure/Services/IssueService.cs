using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Application.DTOs.Dashboard;
using UrbanInfraSystem.Application.DTOs.Issues;
using UrbanInfraSystem.Application.Interfaces;
using UrbanInfraSystem.Domain.Entities;
using UrbanInfraSystem.Domain.Enums;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Services;

public class IssueService : IIssueService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<IssueService> _logger;

    public IssueService(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<IssueService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<ApiResponse<DashboardStatsResponse>> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var resolvedStatusCodes = new[] { "RESOLVED", "CLOSED", "REJECTED" };

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        var activeIssuesCount = await _context.Issues
            .Where(i => i.IsPublic && !i.IsArchived && !resolvedStatusCodes.Contains(i.Status.StatusCode))
            .CountAsync(cancellationToken);

        var resolvedThisWeekCount = await _context.Issues
            .Where(i => i.IsPublic && !i.IsArchived && i.ResolvedAt.HasValue && i.ResolvedAt >= weekStart)
            .CountAsync(cancellationToken);

        var citizenUsersCount = await _context.Users
            .Where(u => _context.UserRoles
                .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == Roles.Citizen))
                .Select(ur => ur.UserId)
                .Contains(u.Id))
            .CountAsync(cancellationToken);

        return new ApiResponse<DashboardStatsResponse>
        {
            Success = true,
            Data = new DashboardStatsResponse
            {
                ActiveIssuesCount = activeIssuesCount,
                ResolvedThisWeekCount = resolvedThisWeekCount,
                CitizenUsersCount = citizenUsersCount
            }
        };
    }

    // â”€â”€â”€ Helper: tÃ­nh KpiItem tá»« giÃ¡ trá»‹ hiá»‡n táº¡i vÃ  ká»³ trÆ°á»›c â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private static KpiItem BuildKpiItem(int current, int previous)
    {
        double? trendPercent = null;
        var direction = "stable";

        if (previous > 0)
        {
            trendPercent = Math.Round((current - previous) / (double)previous * 100, 1);
            direction = trendPercent > 0 ? "up" : trendPercent < 0 ? "down" : "stable";
        }
        else if (current > 0)
        {
            trendPercent = 100;
            direction = "up";
        }

        return new KpiItem
        {
            Value = current,
            TrendPercent = trendPercent,
            TrendDirection = direction
        };
    }

    public async Task<ApiResponse<AdminKpiResponse>> GetAdminKpiAsync(
        CancellationToken cancellationToken = default)
    {
        // â”€â”€ Má»‘c thá»i gian â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var now = DateTime.UtcNow;

        // Tuáº§n nÃ y: tá»« thá»© Hai (ISO) Ä‘áº¿n nay
        var daysFromMonday = ((int)now.DayOfWeek + 6) % 7; // Mon = 0
        var thisWeekStart = now.Date.AddDays(-daysFromMonday);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var lastWeekEnd   = thisWeekStart;

        // HÃ´m nay & hÃ´m qua
        var todayStart     = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);

        var resolvedCodes = new[] { "RESOLVED", "CLOSED", "REJECTED" };

        // â”€â”€ 1. Total Users (IsActive = true) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var totalUsersNow  = await _context.Users.CountAsync(u => u.IsActive, cancellationToken);
        // Proxy cho "tuáº§n trÆ°á»›c": user táº¡o trÆ°á»›c tuáº§n nÃ y (Ä‘Ã¢y lÃ  con sá»‘ cÃ³ tÃ­nh á»•n Ä‘á»‹nh hÆ¡n)
        var totalUsersPrev = await _context.Users
            .CountAsync(u => u.IsActive && u.CreatedAtUtc < thisWeekStart, cancellationToken);
        var totalUsersCurrent = totalUsersNow; // all active users lÃ  hiá»‡n táº¡i
        // Trend: user má»›i tuáº§n nÃ y vs tuáº§n trÆ°á»›c
        var newUsersThisWeek = await _context.Users
            .CountAsync(u => u.CreatedAtUtc >= thisWeekStart, cancellationToken);
        var newUsersLastWeek = await _context.Users
            .CountAsync(u => u.CreatedAtUtc >= lastWeekStart && u.CreatedAtUtc < lastWeekEnd, cancellationToken);

        // â”€â”€ 2. Open Incidents â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var openNow = await _context.Issues
            .CountAsync(i => !i.IsArchived && !resolvedCodes.Contains(i.Status.StatusCode), cancellationToken);
        // Tuáº§n trÆ°á»›c: sá»± cá»‘ Ä‘ang má»Ÿ táº¡i thá»i Ä‘iá»ƒm cuá»‘i tuáº§n trÆ°á»›c (gáº§n Ä‘Ãºng báº±ng cÃ¡ch Ä‘áº¿m má»Ÿ trÆ°á»›c lastWeekEnd & chÆ°a resolved hoáº·c resolved sau lastWeekEnd)
        var openLastWeek = await _context.Issues
            .CountAsync(i => !i.IsArchived
                && i.ReportedAt < lastWeekEnd
                && (!resolvedCodes.Contains(i.Status.StatusCode)
                    || (i.ResolvedAt.HasValue && i.ResolvedAt >= lastWeekEnd)),
            cancellationToken);

        // â”€â”€ 3. Active Departments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var deptNow  = await _context.Departments.CountAsync(d => d.IsActive, cancellationToken);
        var deptPrev = await _context.Departments
            .CountAsync(d => d.IsActive && d.CreatedAt < thisWeekStart, cancellationToken);

        // â”€â”€ 4. Resolved This Week â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var resolvedThisWeek = await _context.Issues
            .CountAsync(i => !i.IsArchived && i.ResolvedAt.HasValue && i.ResolvedAt >= thisWeekStart, cancellationToken);
        var resolvedLastWeek = await _context.Issues
            .CountAsync(i => !i.IsArchived && i.ResolvedAt.HasValue
                && i.ResolvedAt >= lastWeekStart && i.ResolvedAt < lastWeekEnd, cancellationToken);

        // â”€â”€ 5. New Today (vs hÃ´m qua) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var newToday     = await _context.Issues
            .CountAsync(i => !i.IsArchived && i.ReportedAt >= todayStart, cancellationToken);
        var newYesterday = await _context.Issues
            .CountAsync(i => !i.IsArchived && i.ReportedAt >= yesterdayStart && i.ReportedAt < todayStart, cancellationToken);

        // â”€â”€ 6. Critical Incidents (open + priority CRITICAL) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var criticalNow = await _context.Issues
            .CountAsync(i => !i.IsArchived
                && !resolvedCodes.Contains(i.Status.StatusCode)
                && i.Priority.PriorityCode == "CRITICAL", cancellationToken);
        var criticalLastWeek = await _context.Issues
            .CountAsync(i => !i.IsArchived
                && i.ReportedAt < lastWeekEnd
                && i.Priority.PriorityCode == "CRITICAL"
                && (!resolvedCodes.Contains(i.Status.StatusCode)
                    || (i.ResolvedAt.HasValue && i.ResolvedAt >= lastWeekEnd)),
            cancellationToken);

        // â”€â”€ Build response â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        return new ApiResponse<AdminKpiResponse>
        {
            Success = true,
            Data = new AdminKpiResponse
            {
                TotalUsers         = BuildKpiItem(totalUsersCurrent, totalUsersPrev),
                OpenIncidents      = BuildKpiItem(openNow,          openLastWeek),
                ActiveDepartments  = BuildKpiItem(deptNow,          deptPrev),
                ResolvedThisWeek   = BuildKpiItem(resolvedThisWeek, resolvedLastWeek),
                NewToday           = BuildKpiItem(newToday,         newYesterday),
                CriticalIncidents  = BuildKpiItem(criticalNow,      criticalLastWeek),
            }
        };
    }

    public async Task<ApiResponse<IReadOnlyList<TrendDataPoint>>> GetIncidentTrendsAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        List<TrendDataPoint> points;

        switch (period.Trim())
        {
            // â”€â”€ ThisWeek: 7 ngÃ y (Mon â†’ Sun cá»§a tuáº§n hiá»‡n táº¡i) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            case "ThisWeek":
            {
                var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
                var weekStart = now.Date.AddDays(-daysFromMonday);

                // Group theo DayOfWeek (0=Sun,1=Mon,...6=Sat) trong pháº¡m vi tuáº§n
                var raw = await _context.Issues
                    .Where(i => !i.IsArchived && i.ReportedAt >= weekStart && i.ReportedAt < weekStart.AddDays(7))
                    .GroupBy(i => i.ReportedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var dayLabels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                points = Enumerable.Range(0, 7)
                    .Select(offset =>
                    {
                        var date = weekStart.AddDays(offset);
                        var label = dayLabels[offset];
                        var count = raw.FirstOrDefault(r => r.Date == date)?.Count ?? 0;
                        return new TrendDataPoint { Label = label, Value = count };
                    })
                    .ToList();
                break;
            }

            // â”€â”€ ThisMonth: tá»«ng ngÃ y trong thÃ¡ng hiá»‡n táº¡i â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            case "ThisMonth":
            {
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var monthEnd = monthStart.AddMonths(1);

                var raw = await _context.Issues
                    .Where(i => !i.IsArchived && i.ReportedAt >= monthStart && i.ReportedAt < monthEnd)
                    .GroupBy(i => i.ReportedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                points = Enumerable.Range(1, daysInMonth)
                    .Select(day =>
                    {
                        var date = new DateTime(now.Year, now.Month, day);
                        var count = raw.FirstOrDefault(r => r.Date == date)?.Count ?? 0;
                        return new TrendDataPoint { Label = day.ToString(), Value = count };
                    })
                    .ToList();
                break;
            }

            // â”€â”€ ThisYear: 12 thÃ¡ng â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            case "ThisYear":
            default:
            {
                var yearStart = new DateTime(now.Year, 1, 1);
                var yearEnd   = yearStart.AddYears(1);

                var raw = await _context.Issues
                    .Where(i => !i.IsArchived && i.ReportedAt >= yearStart && i.ReportedAt < yearEnd)
                    .GroupBy(i => i.ReportedAt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var monthLabels = new[] { "Jan","Feb","Mar","Apr","May","Jun",
                                          "Jul","Aug","Sep","Oct","Nov","Dec" };
                points = Enumerable.Range(1, 12)
                    .Select(m => new TrendDataPoint
                    {
                        Label = monthLabels[m - 1],
                        Value = raw.FirstOrDefault(r => r.Month == m)?.Count ?? 0
                    })
                    .ToList();
                break;
            }
        }

        return new ApiResponse<IReadOnlyList<TrendDataPoint>>
        {
            Success = true,
            Data = points
        };
    }

    public async Task<ApiResponse<IReadOnlyList<CategoryDistributionPoint>>> GetCategoryDistributionAsync(CancellationToken cancellationToken = default)
    {
        var issues = await _context.Issues
            .Include(i => i.IssueType)
            .Where(i => !i.IsArchived)
            .ToListAsync(cancellationToken);

        var totalIssues = issues.Count;
        if (totalIssues == 0)
        {
            return new ApiResponse<IReadOnlyList<CategoryDistributionPoint>>
            {
                Success = true,
                Data = new List<CategoryDistributionPoint>()
            };
        }

        var distribution = issues
            .GroupBy(i => i.IssueType.TypeName)
            .Select(g => new CategoryDistributionPoint
            {
                Category = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((double)g.Count() / totalIssues * 100, 1)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new ApiResponse<IReadOnlyList<CategoryDistributionPoint>>
        {
            Success = true,
            Data = distribution
        };
    }

    public async Task<ApiResponse<IReadOnlyList<HeatmapDataPoint>>> GetIncidentHeatmapAsync(string timeframe, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startDate = timeframe.ToLower() switch
        {
            "24h" => now.AddHours(-24),
            "30d" => now.AddDays(-30),
            "7d" or _ => now.AddDays(-7)
        };

        var issues = await _context.Issues
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Where(i => !i.IsArchived && i.ReportedAt >= startDate)
            .ToListAsync(cancellationToken);

        // Group by Area
        var heatmapData = issues
            .GroupBy(i => new { i.Area.AreaCode, i.Area.AreaName })
            .Select(g =>
            {
                // Find dominant severity in the group
                var dominantSeverity = g.GroupBy(i => i.Priority.PriorityCode)
                    .OrderByDescending(x => x.Count())
                    .ThenBy(x => x.First().Priority.SeverityRank) // tie-breaker: higher severity
                    .Select(x => x.Key)
                    .FirstOrDefault() ?? "LOW";

                return new HeatmapDataPoint
                {
                    DistrictId = g.Key.AreaCode,
                    DistrictName = g.Key.AreaName,
                    Severity = dominantSeverity,
                    Count = g.Count()
                };
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new ApiResponse<IReadOnlyList<HeatmapDataPoint>>
        {
            Success = true,
            Data = heatmapData
        };
    }

    public async Task<ApiResponse<IReadOnlyList<AuditLogResponse>>> GetRecentAuditLogsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(u => u.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Fetch user emails manually if needed
        var userIds = logs.Where(l => l.ActorUserId != null).Select(l => l.ActorUserId).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var response = logs.Select(l => new AuditLogResponse
        {
            Id = l.Id,
            Title = l.Action + (l.EntityName != null ? $" on {l.EntityName}" : ""),
            Subtitle = l.ActorUserId != null && users.ContainsKey(l.ActorUserId) ? $"User: {users[l.ActorUserId]}" : "System Auto-Alert",
            Timestamp = l.OccurredAt,
            Type = "info"
        }).ToList();

        return new ApiResponse<IReadOnlyList<AuditLogResponse>>
        {
            Success = true,
            Data = response
        };
    }

    public async Task<ApiResponse<IssueDetailResponse>> CreateIssueAsync(
        CreateIssueFormRequest request,
        string reporterId,
        CancellationToken cancellationToken = default)
    {
        var issueTypeIds = request.IssueTypeIds
            .Concat(request.IssueTypeId.HasValue ? [request.IssueTypeId.Value] : [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (issueTypeIds.Count == 0 || issueTypeIds.Count > 5)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Vui lÃ²ng chá»n tá»« 1 Ä‘áº¿n 5 loáº¡i sá»± cá»‘."
            };
        }

        var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == reporterId, cancellationToken);
        if (reporter == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "TÃ i khoáº£n cÃ´ng dÃ¢n khÃ´ng tá»“n táº¡i."
            };
        }

        var issueTypes = await _context.IssueTypes
            .Where(t => issueTypeIds.Contains(t.IssueTypeId) && t.IsActive)
            .ToListAsync(cancellationToken);
        if (issueTypes.Count != issueTypeIds.Count)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Má»™t hoáº·c nhiá»u loáº¡i sá»± cá»‘ khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ vÃ´ hiá»‡u hÃ³a."
            };
        }

        var containsOtherType = issueTypes.Any(t =>
            string.Equals(t.TypeCode, "OTHER", StringComparison.OrdinalIgnoreCase));
        if (containsOtherType && string.IsNullOrWhiteSpace(request.CustomTypeDescription))
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Vui lÃ²ng mÃ´ táº£ cá»¥ thá»ƒ khi chá»n loáº¡i sá»± cá»‘ OTHER."
            };
        }

        var area = await _context.Areas
            .FirstOrDefaultAsync(a => a.AreaId == request.AreaId && a.IsActive, cancellationToken);
        if (area == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = $"Khu vá»±c (ID: {request.AreaId}) khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ vÃ´ hiá»‡u hÃ³a."
            };
        }

        IssuePriority? priority = null;
        if (request.PriorityId.HasValue)
        {
            priority = await _context.IssuePriorities
                .FirstOrDefaultAsync(p => p.PriorityId == request.PriorityId.Value && p.IsActive, cancellationToken);
            if (priority == null)
            {
                return new ApiResponse<IssueDetailResponse>
                {
                    Success = false,
                    Message = $"Má»©c Ä‘á»™ Æ°u tiÃªn (ID: {request.PriorityId.Value}) khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ vÃ´ hiá»‡u hÃ³a."
                };
            }
        }
        else
        {
            priority = await _context.IssuePriorities
                .Where(p => p.IsActive)
                .OrderBy(p => p.SeverityRank)
                .FirstOrDefaultAsync(cancellationToken);

            if (priority == null)
            {
                return new ApiResponse<IssueDetailResponse>
                {
                    Success = false,
                    Message = "Há»‡ thá»‘ng chÆ°a cáº¥u hÃ¬nh má»©c Ä‘á»™ Æ°u tiÃªn máº·c Ä‘á»‹nh."
                };
            }
        }

        var status = await _context.IssueStatuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (status == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = "Há»‡ thá»‘ng chÆ°a cáº¥u hÃ¬nh tráº¡ng thÃ¡i sá»± cá»‘."
            };
        }

        string reportPublicCode;
        do
        {
            var randomSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            reportPublicCode = $"REP-{DateTime.UtcNow:yyyyMMdd}-{randomSuffix}";
        }
        while (await _context.Reports.AnyAsync(r => r.PublicCode == reportPublicCode, cancellationToken));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var reportedAt = DateTime.UtcNow;
        var report = new Report
        {
            ReporterId = reporterId,
            AreaId = area.AreaId,
            PublicCode = reportPublicCode,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AddressText = request.AddressText?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsPublic = true,
            ReportedAt = reportedAt,
            CreatedAt = reportedAt
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        // Táº¡o liÃªn káº¿t Report - IssueType
        foreach (var issueType in issueTypes)
        {
            _context.ReportIssueTypes.Add(new ReportIssueType
            {
                ReportId = report.ReportId,
                IssueTypeId = issueType.IssueTypeId,
                IssueTypeName = issueType.TypeName,
                IssueTypeCode = issueType.TypeCode,
                CreatedAt = reportedAt
            });
        }

        var issues = issueTypeIds.Select((typeId, index) => new Issue
        {
            ReportId = report.ReportId,
            PublicCode = $"{reportPublicCode}-{index + 1:00}",
            ReporterId = reporterId,
            IssueTypeId = typeId,
            AreaId = area.AreaId,
            PriorityId = priority.PriorityId,
            StatusId = status.StatusId,
            CustomTypeDescription = issueTypes.First(t => t.IssueTypeId == typeId).TypeCode
                .Equals("OTHER", StringComparison.OrdinalIgnoreCase)
                    ? request.CustomTypeDescription?.Trim()
                    : null,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AddressText = request.AddressText?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsPublic = true,
            ReportedAt = reportedAt
        }).ToList();

        _context.Issues.AddRange(issues);
        await _context.SaveChangesAsync(cancellationToken);

        var policies = await _context.SlaPolicies
            .Where(s => issueTypeIds.Contains(s.IssueTypeId) && s.PriorityId == priority.PriorityId)
            .ToListAsync(cancellationToken);

        var initialUpdates = new Dictionary<long, IssueUpdate>();
        foreach (var issue in issues)
        {
            var policy = policies.FirstOrDefault(p => p.IssueTypeId == issue.IssueTypeId);
            var firstResponseMinutes = policy?.FirstResponseMinutes ?? 120;
            var resolutionMinutes = policy?.ResolutionMinutes ?? 1440;
            _context.IssueSlas.Add(new IssueSla
            {
                IssueId = issue.IssueId,
                SlaPolicyId = policy?.Id,
                FirstResponseMinutes = firstResponseMinutes,
                ResolutionMinutes = resolutionMinutes,
                FirstResponseDueAt = reportedAt.AddMinutes(firstResponseMinutes),
                ResolutionDueAt = reportedAt.AddMinutes(resolutionMinutes),
                CreatedAt = reportedAt
            });

            var typeName = issueTypes.First(t => t.IssueTypeId == issue.IssueTypeId).TypeName;
            var initialUpdate = new IssueUpdate
            {
                IssueId = issue.IssueId,
                ToStatusId = status.StatusId,
                Note = $"Report {report.PublicCode} Ä‘Ã£ táº¡o issue {issue.PublicCode} ({typeName}).",
                CreatedBy = reporterId,
                IsSystemGenerated = true,
                CreatedAt = reportedAt
            };
            initialUpdates[issue.IssueId] = initialUpdate;
            _context.IssueUpdates.Add(initialUpdate);

            var routingRule = await _context.RoutingRules
                .Include(r => r.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IssueTypeId == issue.IssueTypeId &&
                                          r.AreaId == issue.AreaId && r.IsActive && r.Department.IsActive,
                    cancellationToken);
            if (routingRule is not null)
            {
                _context.IssueAssignments.Add(new IssueAssignment
                {
                    IssueId = issue.IssueId,
                    DepartmentId = routingRule.DepartmentId,
                    RoutingRuleId = routingRule.RoutingRuleId,
                    AssignmentMethod = "AUTO",
                    AssignmentNote = "Tá»± Ä‘á»™ng Ä‘á»‹nh tuyáº¿n theo loáº¡i sá»± cá»‘ vÃ  khu vá»±c.",
                    AssignedAt = reportedAt,
                    IsCurrent = true
                });
                _context.IssueUpdates.Add(new IssueUpdate
                {
                    IssueId = issue.IssueId,
                    FromStatusId = status.StatusId,
                    ToStatusId = status.StatusId,
                    Note = $"Issue {issue.PublicCode} cá»§a report {report.PublicCode} Ä‘Ã£ Ä‘á»‹nh tuyáº¿n Ä‘áº¿n '{routingRule.Department.DepartmentName}'.",
                    CreatedBy = reporterId,
                    IsSystemGenerated = true,
                    CreatedAt = reportedAt
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = reporterId,
                Action = "Create Issue",
                EntityName = "Issues",
                EntityId = issue.PublicCode,
                OccurredAt = reportedAt
            });
        }

        // Láº¥y ID cá»§a update khá»Ÿi táº¡o trÆ°á»›c khi lÆ°u attachment.
        await _context.SaveChangesAsync(cancellationToken);

        if (request.Images != null && request.Images.Count > 0)
        {
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRoot, "uploads", "issues");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var image in request.Images)
            {
                if (image.Length == 0) continue;

                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream, cancellationToken);
                }

                var fileUrl = $"/uploads/issues/{uniqueFileName}";
                var attachment = new IssueAttachment(initialUpdates[issues[0].IssueId].Id)
                {
                    // áº¢nh gá»‘c thuá»™c Report; trong schema chuyá»ƒn tiáº¿p gáº¯n vÃ o Issue Ä‘áº§u tiÃªn.
                    IssueId = issues[0].IssueId,
                    UploadedBy = reporterId,
                    Kind = "image",
                    FileUrl = fileUrl,
                    ThumbnailUrl = fileUrl,
                    MimeType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType,
                    FileSizeBytes = image.Length,
                    CreatedAt = DateTime.UtcNow
                };

                _context.IssueAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var detailResult = await GetIssueByIdAsync(issues[0].IssueId, reporterId, cancellationToken);
        return new ApiResponse<IssueDetailResponse>
        {
            Success = true,
            Message = $"Táº¡o report {report.PublicCode} vá»›i {issues.Count} issue thÃ nh cÃ´ng.",
            Data = detailResult.Data
        };
    }

    public async Task<ApiResponse<IssueDetailResponse>> GetIssueByIdAsync(
        long issueId,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var issue = await _context.Issues
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Assignments)
            .Include(i => i.Sla)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.IssueId == issueId, cancellationToken);

        if (issue == null)
        {
            return new ApiResponse<IssueDetailResponse>
            {
                Success = false,
                Message = $"KhÃ´ng tÃ¬m tháº¥y bÃ¡o cÃ¡o sá»± cá»‘ cÃ³ ID = {issueId}"
            };
        }

        var reporter = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == issue.ReporterId, cancellationToken);

        bool hasUpvoted = false;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            hasUpvoted = await _context.IssueUpvotes
                .AnyAsync(u => u.IssueId == issue.IssueId && u.UserId == currentUserId, cancellationToken);
        }

        var response = MapToDetailResponse(issue, reporter?.FullName ?? "CÃ´ng dÃ¢n", hasUpvoted);

        var currentDepartment = await _context.IssueAssignments
            .Where(x => x.IssueId == issueId && x.IsCurrent)
            .Select(x => new LookupItemResponse
            {
                Id = x.DepartmentId,
                Name = x.Department.DepartmentName,
                Code = x.Department.DepartmentCode
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        response.CurrentDepartment = currentDepartment;

        return new ApiResponse<IssueDetailResponse>
        {
            Success = true,
            Data = response
        };
    }

    public async Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> SearchIssuesAsync(
        SearchIssuesRequest request,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Issues
            .IgnoreQueryFilters()
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Assignments)
            .Where(i => i.IsPublic && !i.IsArchived)
            .AsNoTracking();

        if (request.IssueTypeId.HasValue)
        {
            query = query.Where(i => i.IssueTypeId == request.IssueTypeId.Value);
        }

        if (request.AreaId.HasValue)
        {
            query = query.Where(i => i.AreaId == request.AreaId.Value);
        }

        if (request.StatusCodes != null && request.StatusCodes.Length > 0)
        {
            var codes = request.StatusCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Status.StatusCode.ToUpper()));
        }

        if (request.PriorityCodes != null && request.PriorityCodes.Length > 0)
        {
            var codes = request.PriorityCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Priority.PriorityCode.ToUpper()));
        }

        if (request.From.HasValue)
        {
            query = query.Where(i => i.ReportedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(i => i.ReportedAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(keyword) ||
                                     i.Description.ToLower().Contains(keyword) ||
                                     i.PublicCode.ToLower().Contains(keyword));
        }

        if (request.MinLatitude.HasValue) query = query.Where(i => i.Latitude >= request.MinLatitude.Value);
        if (request.MaxLatitude.HasValue) query = query.Where(i => i.Latitude <= request.MaxLatitude.Value);
        if (request.MinLongitude.HasValue) query = query.Where(i => i.Longitude >= request.MinLongitude.Value);
        if (request.MaxLongitude.HasValue) query = query.Where(i => i.Longitude <= request.MaxLongitude.Value);

        var totalItems = await query.LongCountAsync(cancellationToken);

        query = request.Sort?.ToLower() switch
        {
            "reportedatasc" => query.OrderBy(i => i.ReportedAt),
            "upvotedesc" => query.OrderByDescending(i => i.UpvoteCount),
            _ => query.OrderByDescending(i => i.ReportedAt)
        };

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var issues = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        HashSet<long> upvotedIssueIds = [];
        if (!string.IsNullOrEmpty(currentUserId) && issues.Count > 0)
        {
            var issueIds = issues.Select(i => i.IssueId).ToList();
            var upvotes = await _context.IssueUpvotes
                .Where(u => u.UserId == currentUserId && issueIds.Contains(u.IssueId))
                .Select(u => u.IssueId)
                .ToListAsync(cancellationToken);
            upvotedIssueIds = [.. upvotes];
        }

        var items = issues.Select(i => MapToSummaryResponse(i, upvotedIssueIds.Contains(i.IssueId))).ToList();

        return new ApiResponse<PagedResponse<IssueSummaryResponse>>
        {
            Success = true,
            Data = new PagedResponse<IssueSummaryResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            }
        };
    }

    public async Task<ApiResponse<PagedResponse<IssueSummaryResponse>>> GetMyIssuesAsync(
        GetMyIssuesRequest request,
        string reporterId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Issues
            .IgnoreQueryFilters()
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Assignments)
            .Where(i => i.ReporterId == reporterId)
            .AsNoTracking();

        if (request.StatusCodes != null && request.StatusCodes.Length > 0)
        {
            var codes = request.StatusCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Status.StatusCode.ToUpper()));
        }

        var totalItems = await query.LongCountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var issues = await query
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        HashSet<long> upvotedIssueIds = [];
        if (issues.Count > 0)
        {
            var issueIds = issues.Select(i => i.IssueId).ToList();
            var upvotes = await _context.IssueUpvotes
                .Where(u => u.UserId == reporterId && issueIds.Contains(u.IssueId))
                .Select(u => u.IssueId)
                .ToListAsync(cancellationToken);
            upvotedIssueIds = [.. upvotes];
        }

        var items = issues.Select(i => MapToSummaryResponse(i, upvotedIssueIds.Contains(i.IssueId))).ToList();

        return new ApiResponse<PagedResponse<IssueSummaryResponse>>
        {
            Success = true,
            Data = new PagedResponse<IssueSummaryResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            }
        };
    }

    public async Task<ApiResponse<IReadOnlyList<NearbyIssueResponse>>> FindNearbyIssuesAsync(
        FindNearbyIssuesRequest request,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.AddDays(-request.WithinDays);

        // Debug: log the filter parameters
        _logger.LogDebug("FindNearby: Lat={Lat}, Lng={Lng}, Radius={Radius}, IssueType={IssueTypeId}, FromDate={FromDate}",
            request.Latitude, request.Longitude, request.RadiusMeters, request.IssueTypeId, fromDate);

        var query = _context.Issues
            .IgnoreQueryFilters()
            .Include(i => i.IssueType)
            .Include(i => i.Area)
            .Include(i => i.Priority)
            .Include(i => i.Status)
            .Include(i => i.Attachments)
            .Include(i => i.Assignments)
            .Where(i => i.ReportedAt >= fromDate && i.IsPublic && !i.IsArchived);

        // Apply date range filter if specified
        if (request.FromDate.HasValue)
        {
            query = query.Where(i => i.ReportedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(i => i.ReportedAt <= request.ToDate.Value);
        }

        // Apply status codes filter if specified
        if (request.StatusCodes != null && request.StatusCodes.Length > 0)
        {
            var codes = request.StatusCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Status.StatusCode.ToUpper()));
        }

        // Apply priority codes filter if specified
        if (request.PriorityCodes != null && request.PriorityCodes.Length > 0)
        {
            var codes = request.PriorityCodes.Select(c => c.ToUpper()).ToList();
            query = query.Where(i => codes.Contains(i.Priority.PriorityCode.ToUpper()));
        }

        // Debug: count before type filter
        var totalCount = await query.CountAsync(cancellationToken);
        _logger.LogDebug("Issues matching date+IsPublic: {Count}", totalCount);

        if (request.IssueTypeId.HasValue)
        {
            query = query.Where(i => i.IssueTypeId == request.IssueTypeId.Value);
        }

        var issues = await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Issues after type filter: {Count}", issues.Count);

        var nearbyList = new List<NearbyIssueResponse>();

        foreach (var issue in issues)
        {
            var distance = CalculateHaversineDistance(
                (double)request.Latitude, (double)request.Longitude,
                (double)issue.Latitude, (double)issue.Longitude);

            _logger.LogDebug("Issue {Id} ({Title}): distance={Distance}m, within radius={Within}",
                issue.IssueId, issue.Title, distance, distance <= request.RadiusMeters ? "YES" : "NO");

            if (distance <= request.RadiusMeters)
            {
                bool hasUpvoted = false;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    hasUpvoted = await _context.IssueUpvotes
                        .AnyAsync(u => u.IssueId == issue.IssueId && u.UserId == currentUserId, cancellationToken);
                }

                var summary = MapToSummaryResponse(issue, hasUpvoted);
                nearbyList.Add(new NearbyIssueResponse
                {
                    Id = summary.Id,
                    PublicCode = summary.PublicCode,
                    Title = summary.Title,
                    IssueType = summary.IssueType,
                    Area = summary.Area,
                    Priority = summary.Priority,
                    Status = summary.Status,
                    Latitude = summary.Latitude,
                    Longitude = summary.Longitude,
                    ThumbnailUrl = summary.ThumbnailUrl,
                    UpvoteCount = summary.UpvoteCount,
                    HasUpvoted = summary.HasUpvoted,
                    ReportedAt = summary.ReportedAt,
                    DistanceMeters = Math.Round(distance, 1)
                });
            }
        }

        var result = nearbyList
            .OrderBy(n => n.DistanceMeters)
            .Take(request.Limit)
            .ToList();

        _logger.LogInformation("FindNearby: returning {Count} issues within radius", result.Count);

        return new ApiResponse<IReadOnlyList<NearbyIssueResponse>>
        {
            Success = true,
            Data = result
        };
    }

    public async Task<ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>> GetIssueTimelineAsync(
        long issueId,
        CancellationToken cancellationToken = default)
    {
        var issueExists = await _context.Issues.AnyAsync(i => i.IssueId == issueId, cancellationToken);
        if (!issueExists)
        {
            return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>
            {
                Success = false,
                Message = $"KhÃ´ng tÃ¬m tháº¥y bÃ¡o cÃ¡o sá»± cá»‘ cÃ³ ID = {issueId}"
            };
        }

        var updates = await _context.IssueUpdates
            .Include(u => u.FromStatus)
            .Include(u => u.ToStatus)
            .Include(u => u.Attachments)
            .Where(u => u.IssueId == issueId)
            .OrderBy(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var timelineItems = updates.Select(u => new IssueTimelineItemResponse
        {
            Id = u.Id,
            UpdateType = u.Note != null && u.Note.StartsWith("ÄÃ£ Ä‘á»‹nh tuyáº¿n")
                ? "ROUTED"
                : u.Note != null && u.Note.StartsWith("ÄÃ£ chuyá»ƒn Ä‘Æ¡n vá»‹")
                    ? "REASSIGNED"
                    : u.FromStatusId == null ? "CREATED" : "STATUS_CHANGE",
            FromStatus = u.FromStatus != null ? new LookupItemResponse
            {
                Id = u.FromStatus.StatusId,
                Name = u.FromStatus.StatusName,
                Code = u.FromStatus.StatusCode
            } : null,
            ToStatus = u.ToStatus != null ? new LookupItemResponse
            {
                Id = u.ToStatus.StatusId,
                Name = u.ToStatus.StatusName,
                Code = u.ToStatus.StatusCode
            } : null,
            ProgressPercent = u.ProgressPercent,
            Note = u.Note,
            IsSystemGenerated = u.IsSystemGenerated,
            CreatedAt = u.CreatedAt,
            Attachments = u.Attachments.Select(MapAttachment).ToList()
        }).ToList();

        return new ApiResponse<IReadOnlyList<IssueTimelineItemResponse>>
        {
            Success = true,
            Data = timelineItems
        };
    }

    private static IssueSummaryResponse MapToSummaryResponse(Issue issue, bool hasUpvoted)
    {
        var firstAttachment = issue.Attachments.FirstOrDefault();
        return new IssueSummaryResponse
        {
            Id = issue.IssueId,
            PublicCode = issue.PublicCode,
            Title = issue.Title,
            IssueType = new LookupItemResponse
            {
                Id = issue.IssueType.IssueTypeId,
                Name = issue.IssueType.TypeName,
                Code = issue.IssueType.TypeCode
            },
            Area = new LookupItemResponse
            {
                Id = issue.Area.AreaId,
                Name = issue.Area.AreaName,
                Code = issue.Area.AreaCode
            },
            Priority = new LookupItemResponse
            {
                Id = issue.Priority.PriorityId,
                Name = issue.Priority.PriorityName,
                Code = issue.Priority.PriorityCode
            },
            Status = new LookupItemResponse
            {
                Id = issue.Status.StatusId,
                Name = issue.Status.StatusName,
                Code = issue.Status.StatusCode
            },
            Latitude = issue.Latitude,
            Longitude = issue.Longitude,
            ThumbnailUrl = firstAttachment?.ThumbnailUrl ?? firstAttachment?.FileUrl,
            UpvoteCount = issue.UpvoteCount,
            HasUpvoted = hasUpvoted,
            ReportedAt = issue.ReportedAt,
            IsAssigned = issue.Assignments.Any(a => a.IsCurrent)
        };
    }

    private static IssueDetailResponse MapToDetailResponse(Issue issue, string reporterDisplayName, bool hasUpvoted)
    {
        var summary = MapToSummaryResponse(issue, hasUpvoted);
        return new IssueDetailResponse
        {
            Id = summary.Id,
            PublicCode = summary.PublicCode,
            Title = summary.Title,
            IssueType = summary.IssueType,
            Area = summary.Area,
            Priority = summary.Priority,
            Status = summary.Status,
            Latitude = summary.Latitude,
            Longitude = summary.Longitude,
            ThumbnailUrl = summary.ThumbnailUrl,
            UpvoteCount = summary.UpvoteCount,
            HasUpvoted = summary.HasUpvoted,
            ReportedAt = summary.ReportedAt,
            Description = issue.Description,
            AddressText = issue.AddressText,
            ReporterDisplayName = reporterDisplayName,
            ResolvedAt = issue.ResolvedAt,
            ClosedAt = issue.ClosedAt,
            UpdatedAt = issue.ReportedAt,
            Sla = issue.Sla != null ? new IssueSlaResponse
            {
                Id = issue.Sla.Id,
                IssueId = issue.Sla.IssueId,
                SlaPolicyId = issue.Sla.SlaPolicyId,
                FirstResponseMinutes = issue.Sla.FirstResponseMinutes,
                ResolutionMinutes = issue.Sla.ResolutionMinutes,
                FirstResponseDueAt = issue.Sla.FirstResponseDueAt,
                ResolutionDueAt = issue.Sla.ResolutionDueAt,
                FirstRespondedAt = issue.Sla.FirstRespondedAt,
                ResolvedAt = issue.Sla.ResolvedAt,
                IsFirstResponseBreached = issue.Sla.IsFirstResponseBreached,
                IsResolutionBreached = issue.Sla.IsResolutionBreached
            } : null,
            Attachments = issue.Attachments.Select(MapAttachment).ToList()
        };
    }

    private static IssueAttachmentResponse MapAttachment(IssueAttachment a)
    {
        return new IssueAttachmentResponse
        {
            Id = a.Id,
            Kind = a.Kind,
            FileUrl = a.FileUrl,
            ThumbnailUrl = a.ThumbnailUrl,
            MimeType = a.MimeType,
            FileSizeBytes = a.FileSizeBytes,
            WidthPx = a.WidthPx,
            HeightPx = a.HeightPx,
            CreatedAt = a.CreatedAt
        };
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle) => (Math.PI / 180) * angle;
}

