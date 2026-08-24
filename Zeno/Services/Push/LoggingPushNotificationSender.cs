using Zeno.Application.Interfaces;
using Zeno.Application.Notifications;

namespace Zeno.Services.Push;

/// <summary>
/// Usado quando nao ha credencial de push configurada. Registra o que seria enviado em vez de falhar,
/// e reporta IsConfigured = false para que a API consiga dizer ao app que nenhum envio sai.
/// </summary>
public class LoggingPushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<LoggingPushNotificationSender> _logger;

    public LoggingPushNotificationSender(ILogger<LoggingPushNotificationSender> logger)
    {
        _logger = logger;
    }

    public bool IsConfigured => false;

    public Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Push nao configurado. Notificacao descartada: '{Title}' - '{Body}' ({Count} aparelho(s)).",
            message.Title, message.Body, tokens.Count);

        return Task.FromResult(PushSendResult.Empty);
    }
}
