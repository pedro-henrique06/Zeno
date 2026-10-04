using MongoDB.Driver;
using Zeno.Domain.Capture;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CaptureRuleRepository : ICaptureRuleRepository
{
    private readonly ZenoMongoContext _context;

    public CaptureRuleRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CaptureRule>> GetByUserAsync(Guid userId)
    {
        return await _context.CaptureRules
            .Find(x => x.UserId == userId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<CaptureRule> CreateAsync(CaptureRule rule)
    {
        await _context.CaptureRules.InsertOneAsync(rule);
        return rule;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var result = await _context.CaptureRules.DeleteOneAsync(x => x.Id == id && x.UserId == userId);
        return result.DeletedCount > 0;
    }
}
