using Zeno.Domain.Push;

namespace Zeno.Application.Interfaces;

public interface IPushNotificationService
{
    Task SubscribeAsync(Guid userId, PushSubscription subscription);
    Task UnsubscribeAsync(Guid userId, string endpoint);
    Task SendDailyBalancesAsync();
}
