using MongoDB.Driver;
using Zeno.Domain.Entry;
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
        var filter = builder.Gte(x => x.Date, startDate) & builder.Lt(x => x.Date, endDate);

        var totalCount = await _context.Entries.CountDocumentsAsync(filter);

        var items = await _context.Entries
            .Find(filter)
            .SortByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, (int)totalCount);
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
}
