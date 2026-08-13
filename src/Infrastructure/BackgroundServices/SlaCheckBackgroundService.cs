using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.BackgroundServices;

/// <summary>
/// BackgroundService kiểm tra định kỳ các Issue_SLAs chưa hoàn thành,
/// phát hiện và cảnh báo các sự cố bị vi phạm / sắp vi phạm SLA.
/// </summary>
public class SlaCheckBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaCheckBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

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
                await CheckBreachedSlasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SlaCheckBackgroundService] An error occurred while checking Issue SLAs.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("[SlaCheckBackgroundService] SLA Monitor Background Job stopping.");
    }

    private async Task CheckBreachedSlasAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Quét tất cả IssueSla chưa giải quyết xong (ResolvedAt == null)
        var uncompletedSlas = await context.IssueSlas
            .Include(s => s.Issue)
            .Where(s => s.ResolvedAt == null)
            .ToListAsync(stoppingToken);

        if (uncompletedSlas.Count == 0)
        {
            _logger.LogInformation("[SlaCheckBackgroundService] No active uncompleted Issue SLAs found at {Now}.", now);
            return;
        }

        var breachedResolutionList = uncompletedSlas
            .Where(s => s.ResolutionDueAt < now)
            .ToList();

        var breachedResponseList = uncompletedSlas
            .Where(s => s.FirstResponseDueAt.HasValue && s.FirstResponseDueAt.Value < now && s.FirstRespondedAt == null)
            .ToList();

        _logger.LogInformation(
            "[SlaCheckBackgroundService] SLA Check at {Now}: Total Active = {TotalActive}, Resolution Breached = {ResBreached}, First Response Breached = {RespBreached}",
            now, uncompletedSlas.Count, breachedResolutionList.Count, breachedResponseList.Count);

        bool changesMade = false;

        foreach (var sla in breachedResolutionList)
        {
            _logger.LogWarning(
                "[SLA BREACH - RESOLUTION] Issue ID: {IssueId}, PublicCode: {PublicCode}, ResolutionDueAt: {DueAt}, Delay: {DelayMinutes} mins",
                sla.IssueId,
                sla.Issue?.PublicCode ?? "N/A",
                sla.ResolutionDueAt,
                Math.Round((now - sla.ResolutionDueAt).TotalMinutes, 1));

            if (!sla.IsResolutionBreached)
            {
                sla.IsResolutionBreached = true;
                changesMade = true;
            }
        }

        foreach (var sla in breachedResponseList)
        {
            _logger.LogWarning(
                "[SLA BREACH - FIRST RESPONSE] Issue ID: {IssueId}, PublicCode: {PublicCode}, FirstResponseDueAt: {DueAt}, Delay: {DelayMinutes} mins",
                sla.IssueId,
                sla.Issue?.PublicCode ?? "N/A",
                sla.FirstResponseDueAt,
                Math.Round((now - sla.FirstResponseDueAt!.Value).TotalMinutes, 1));

            if (!sla.IsFirstResponseBreached)
            {
                sla.IsFirstResponseBreached = true;
                changesMade = true;
            }
        }

        if (changesMade)
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
