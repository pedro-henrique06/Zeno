using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Widgets;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class WidgetKeyRepository : IWidgetKeyRepository
{
    private readonly ZenoMongoContext _context;

    public WidgetKeyRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<WidgetKey?> GetByUserAsync(Guid userId)
    {
        return await _context.WidgetKeys.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<WidgetKey?> GetByHashAsync(string keyHash)
    {
        return await _context.WidgetKeys.Find(x => x.KeyHash == keyHash).FirstOrDefaultAsync();
    }

    public async Task<WidgetKey> UpsertAsync(WidgetKey key)
    {
        var filter = Builders<WidgetKey>.Filter.Eq(x => x.UserId, key.UserId);
        await _context.WidgetKeys.ReplaceOneAsync(filter, key, new ReplaceOptions { IsUpsert = true });
        return key;
    }

    public async Task DeleteByUserAsync(Guid userId)
    {
        await _context.WidgetKeys.DeleteOneAsync(x => x.UserId == userId);
    }
}
