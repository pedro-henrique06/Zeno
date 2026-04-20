using Zeno.Application.Interfaces;

namespace Zeno.Services;

public class RecurringSalaryHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RecurringSalaryHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var salaryService = scope.ServiceProvider.GetRequiredService<ISalaryService>();

            await salaryService.ProcessPendingSalaries();

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
