using Zeno.Application.Interfaces;

namespace Zeno.Services;

public class RecurringEntryHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RecurringEntryHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var recurringEntryService = scope.ServiceProvider.GetRequiredService<IRecurringEntryService>();

                await recurringEntryService.ProcessPendingEntries();

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecurringEntryHostedService] Erro: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
