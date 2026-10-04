using MongoDB.Driver;
using Zeno.Domain.Capture;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CaptureKeyRepository : ICaptureKeyRepository
{
    private readonly ZenoMongoContext _context;

    public CaptureKeyRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<CaptureKey?> GetByUserAsync(Guid userId)
    {
        return await _context.CaptureKeys.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<CaptureKey?> GetByHashAsync(string keyHash)
    {
        return await _context.CaptureKeys.Find(x => x.KeyHash == keyHash).FirstOrDefaultAsync();
    }

    public async Task<CaptureKey> UpsertAsync(CaptureKey key)
    {
        var filter = Builders<CaptureKey>.Filter.Eq(x => x.UserId, key.UserId);
        await _context.CaptureKeys.ReplaceOneAsync(filter, key, new ReplaceOptions { IsUpsert = true });
        return key;
    }

    public async Task DeleteByUserAsync(Guid userId)
    {
        await _context.CaptureKeys.DeleteOneAsync(x => x.UserId == userId);
    }
}
