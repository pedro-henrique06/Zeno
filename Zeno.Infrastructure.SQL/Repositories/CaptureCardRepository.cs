using MongoDB.Driver;
using Zeno.Domain.Capture;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CaptureCardRepository : ICaptureCardRepository
{
    private readonly ZenoMongoContext _context;

    public CaptureCardRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CaptureCard>> GetByUserAsync(Guid userId)
    {
        return await _context.CaptureCards
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.LastSeenAt)
            .ToListAsync();
    }

    public async Task<CaptureCard?> GetByIdAsync(Guid userId, Guid id)
    {
        return await _context.CaptureCards.Find(x => x.Id == id && x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<CaptureCard?> GetByNameKeyAsync(Guid userId, string nameKey)
    {
        return await _context.CaptureCards.Find(x => x.UserId == userId && x.NameKey == nameKey).FirstOrDefaultAsync();
    }

    public async Task UpsertAsync(CaptureCard card)
    {
        await _context.CaptureCards.ReplaceOneAsync(x => x.Id == card.Id, card, new ReplaceOptions { IsUpsert = true });
    }

    public async Task DeleteByUserAsync(Guid userId)
    {
        await _context.CaptureCards.DeleteManyAsync(x => x.UserId == userId);
    }
}
