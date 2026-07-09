using Zeno.Domain.Push;

namespace Zeno.Domain.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<List<PushSubscription>> GetByUserIdAsync(Guid userId);
    Task<List<PushSubscription>> GetAllAsync();
    Task UpsertAsync(PushSubscription subscription);
    Task DeleteByEndpointAsync(string endpoint);
}
