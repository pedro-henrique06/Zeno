using Zeno.Application.Interfaces;

namespace Zeno.Services;

/// <summary>
/// Varre as preferencias a cada poucos minutos e envia o resumo diario de quem chegou na hora local configurada.
/// O intervalo curto existe porque a hora de envio e por usuario e depende do fuso dele.
/// </summary>
public class NotificationHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationHostedService> _logger;

    public NotificationHostedService(IServiceProvider serviceProvider, ILogger<NotificationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var sent = await notificationService.SendDueDailyDigestsAsync(stoppingToken);

                if (sent > 0)
                    _logger.LogInformation("Resumo diario enviado para {Count} usuario(s).", sent);

                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na varredura de notificacoes.");
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }
}
