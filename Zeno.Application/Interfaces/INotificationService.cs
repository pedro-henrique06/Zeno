using Zeno.Application.Requests.Notifications;
using Zeno.Application.Responses;

namespace Zeno.Application.Interfaces;

public interface INotificationService
{
    Task<DeviceTokenResponse> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request);
    Task UnregisterDeviceAsync(Guid userId, string token);

    Task<NotificationPreferenceResponse> GetPreferenceAsync(Guid userId);
    Task<NotificationPreferenceResponse> UpdatePreferenceAsync(Guid userId, UpdateNotificationPreferenceRequest request);

    Task<SendTestNotificationResponse> SendTestAsync(Guid userId);

    /// <summary>Varre as preferencias ativas e envia o resumo para quem estiver na hora local configurada. Chamado pelo hosted service.</summary>
    Task<int> SendDueDailyDigestsAsync(CancellationToken cancellationToken = default);
}
