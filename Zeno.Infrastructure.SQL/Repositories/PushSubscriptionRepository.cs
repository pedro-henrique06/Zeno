using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Push;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly ZenoMongoContext _context;

    public PushSubscriptionRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<List<PushSubscription>> GetByUserIdAsync(Guid userId)
    {
        return await _context.PushSubscriptions.Find(x => x.UserId == userId).ToListAsync();
    }

    public async Task<List<PushSubscription>> GetAllAsync()
    {
        return await _context.PushSubscriptions.Find(_ => true).ToListAsync();
    }

    public async Task UpsertAsync(PushSubscription subscription)
    {
        var filter = Builders<PushSubscription>.Filter.Eq(x => x.Endpoint, subscription.Endpoint);
        var options = new ReplaceOptions { IsUpsert = true };
        await _context.PushSubscriptions.ReplaceOneAsync(filter, subscription, options);
    }

    public async Task DeleteByEndpointAsync(string endpoint)
    {
        await _context.PushSubscriptions.DeleteOneAsync(x => x.Endpoint == endpoint);
    }
}
