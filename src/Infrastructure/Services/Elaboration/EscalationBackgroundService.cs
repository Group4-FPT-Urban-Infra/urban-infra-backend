using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace UrbanInfraSystem.Infrastructure.Services.Elaboration;

public class EscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EscalationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // configurable if needed

    public EscalationBackgroundService(IServiceProvider services, ILogger<EscalationBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EscalationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<EscalationProcessor>();
                await processor.ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running SLA breach processor.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("EscalationBackgroundService stopping.");
    }
}
