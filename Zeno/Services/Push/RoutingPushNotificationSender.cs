using Zeno.Application.Interfaces;
using Zeno.Application.Notifications;

namespace Zeno.Services.Push;

/// <summary>
/// Escolhe o provedor pelo formato do token: tokens do Expo vao pelo servico do Expo, os demais (web/FCM)
/// pelo remetente do Firebase, ou pelo de log quando nao ha credencial.
/// </summary>
public class RoutingPushNotificationSender : IPushNotificationSender
{
    private readonly ExpoPushNotificationSender _expo;
    private readonly IPushNotificationSender _fallback;

    public RoutingPushNotificationSender(ExpoPushNotificationSender expo, IPushNotificationSender fallback)
    {
        _expo = expo;
        _fallback = fallback;
    }

    public bool IsConfigured => _expo.IsConfigured || _fallback.IsConfigured;

    public async Task<PushSendResult> SendAsync(IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
    {
        var expoTokens = tokens.Where(ExpoPushNotificationSender.IsExpoToken).ToList();
        var otherTokens = tokens.Where(t => !ExpoPushNotificationSender.IsExpoToken(t)).ToList();

        var expoResult = await _expo.SendAsync(expoTokens, message, cancellationToken);
        // Sem credencial do Firebase os tokens web/FCM nao tem como sair; nao vale nem chamar o remetente de log.
        var otherResult = otherTokens.Count == 0 || !_fallback.IsConfigured
            ? PushSendResult.Empty
            : await _fallback.SendAsync(otherTokens, message, cancellationToken);

        return new PushSendResult
        {
            SuccessCount = expoResult.SuccessCount + otherResult.SuccessCount,
            InvalidTokens = expoResult.InvalidTokens.Concat(otherResult.InvalidTokens).ToList()
        };
    }
}
