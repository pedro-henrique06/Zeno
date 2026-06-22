using MongoDB.Driver;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class EntryRepository : IEntryRepository
{
    private readonly ZenoMongoContext _context;

    public EntryRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<Entry?> GetByIdAsync(Guid id)
    {
        return await _context.Entries.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<Entry> Items, int TotalCount)> GetByMonthForUserPagedAsync(int month, int year, Guid userId, int page, int pageSize)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var builder = Builders<Entry>.Filter;
        var filter = builder.Eq(x => x.UserId, userId) & builder.Gte(x => x.Date, startDate) & builder.Lt(x => x.Date, endDate);

        var totalCount = await _context.Entries.CountDocumentsAsync(filter);

        var items = await _context.Entries
            .Find(filter)
            .SortByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, (int)totalCount);
    }

    public async Task<IEnumerable<Entry>> GetByUserInRangeAsync(Guid userId, DateTime? start, DateTime? end)
    {
        var builder = Builders<Entry>.Filter;
        var filter = builder.Eq(x => x.UserId, userId);

        if (start.HasValue)
            filter &= builder.Gte(x => x.Date, start.Value);

        if (end.HasValue)
            filter &= builder.Lt(x => x.Date, end.Value);

        return await _context.Entries
            .Find(filter)
            .SortBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Entry>> GetRecurringBeforeAsync(Guid userId, DateTime before)
    {
        var builder = Builders<Entry>.Filter;
        var filter = builder.Eq(x => x.UserId, userId) & builder.Eq(x => x.IsRecurring, true) & builder.Lt(x => x.Date, before);

        return await _context.Entries.Find(filter).ToListAsync();
    }

    public async Task<decimal> GetSignedBalanceBeforeAsync(Guid userId, DateTime before)
    {
        var result = await _context.Entries
            .Aggregate()
            .Match(x => x.UserId == userId && x.Date < before)
            .Group(x => 1, g => new { Sum = g.Sum(e => e.Kind == EntryKind.Entrada ? e.Value : -e.Value) })
            .FirstOrDefaultAsync();

        return result?.Sum ?? 0m;
    }

    public async Task<Entry> CreateAsync(Entry entry)
    {
        await _context.Entries.InsertOneAsync(entry);
        return entry;
    }

    public async Task<Entry> UpdateAsync(Entry entry)
    {
        var filter = Builders<Entry>.Filter.Eq(x => x.Id, entry.Id);
        await _context.Entries.ReplaceOneAsync(filter, entry);
        return entry;
    }

    public async Task DeleteAsync(Guid id)
    {
        var filter = Builders<Entry>.Filter.Eq(x => x.Id, id);
        await _context.Entries.DeleteOneAsync(filter);
    }

    public async Task ClearTagReferencesAsync(Guid tagId)
    {
        var filter = Builders<Entry>.Filter.Eq(x => x.TagId, tagId);
        var update = Builders<Entry>.Update.Set(x => x.TagId, null);
        await _context.Entries.UpdateManyAsync(filter, update);
    }

    public async Task MultiplyValuesForUserAsync(Guid userId, decimal factor)
    {
        var filter = Builders<Entry>.Filter.Eq(x => x.UserId, userId);
        var update = Builders<Entry>.Update.Mul(x => x.Value, factor);
        await _context.Entries.UpdateManyAsync(filter, update);
    }
}
