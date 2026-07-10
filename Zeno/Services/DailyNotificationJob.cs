using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zeno.Application.Interfaces;

namespace Zeno.Services;

public class DailyNotificationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyNotificationJob> _logger;

    // Fires at 11:00 UTC (08:00 BRT = UTC-3)
    private static readonly TimeOnly FireTime = new(11, 0, 0);

    public DailyNotificationJob(IServiceProvider serviceProvider, ILogger<DailyNotificationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextFire();
            _logger.LogInformation("[DailyNotificationJob] Next fire in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
            await pushService.SendDailyBalancesAsync();
            _logger.LogInformation("[DailyNotificationJob] Daily balance notifications sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DailyNotificationJob] Failed to send notifications.");
        }
    }

    private static TimeSpan TimeUntilNextFire()
    {
        var now = DateTime.UtcNow;
        var nextFire = now.Date.Add(FireTime.ToTimeSpan());
        if (now >= nextFire)
            nextFire = nextFire.AddDays(1);
        return nextFire - now;
    }
}
