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
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var salaryService = scope.ServiceProvider.GetRequiredService<ISalaryService>();

                await salaryService.ProcessPendingSalaries();

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecurringSalaryHostedService] Erro: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
