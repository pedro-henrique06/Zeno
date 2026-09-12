using Zeno.Application.Notifications;

namespace Zeno.Application.Interfaces;

public interface IPushNotificationSender
{
    /// <summary>Indica se ha credencial configurada. Quando false o envio vira no-op e o servico sabe que nada sai.</summary>
    bool IsConfigured { get; }

    Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default);
}
