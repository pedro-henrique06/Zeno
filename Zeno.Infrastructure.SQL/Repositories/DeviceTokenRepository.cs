using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class DeviceTokenRepository : IDeviceTokenRepository
{
    private readonly ZenoMongoContext _context;

    public DeviceTokenRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeviceToken>> GetActiveByUserAsync(Guid userId)
    {
        return await _context.DeviceTokens
            .Find(x => x.UserId == userId && x.IsActive)
            .SortByDescending(x => x.LastSeenAt)
            .ToListAsync();
    }

    public async Task<DeviceToken?> GetByTokenAsync(string token)
    {
        return await _context.DeviceTokens
            .Find(x => x.Token == token)
            .FirstOrDefaultAsync();
    }

    public async Task<DeviceToken> UpsertAsync(DeviceToken deviceToken)
    {
        // O mesmo aparelho pode trocar de usuario (logout/login), por isso o conflito reatribui o token.
        var filter = Builders<DeviceToken>.Filter.Eq(x => x.Token, deviceToken.Token);
        var update = Builders<DeviceToken>.Update
            .SetOnInsert(x => x.Id, deviceToken.Id)
            .SetOnInsert(x => x.CreatedAt, deviceToken.CreatedAt)
            .Set(x => x.UserId, deviceToken.UserId)
            .Set(x => x.Platform, deviceToken.Platform)
            .Set(x => x.IsActive, true)
            .Set(x => x.LastSeenAt, deviceToken.LastSeenAt);

        var options = new FindOneAndUpdateOptions<DeviceToken>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        return await _context.DeviceTokens.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task DeactivateAsync(string token)
    {
        var update = Builders<DeviceToken>.Update.Set(x => x.IsActive, false);
        await _context.DeviceTokens.UpdateOneAsync(x => x.Token == token, update);
    }

    public async Task DeleteByUserAndTokenAsync(Guid userId, string token)
    {
        await _context.DeviceTokens.DeleteOneAsync(x => x.UserId == userId && x.Token == token);
    }
}
