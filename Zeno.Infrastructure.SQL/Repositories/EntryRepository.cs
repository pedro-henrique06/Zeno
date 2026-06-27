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

        Console.WriteLine($"[DIAG GetByMonth] userId={userId} month={month} year={year} startDate={startDate:O} endDate={endDate:O} totalCount={totalCount} page={page} pageSize={pageSize} itemsCount={items.Count} itemDates=[{string.Join(",", items.Select(i => $"{i.Id}:{i.Date:O}:{i.Kind}"))}]");

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
        // Value é criptografado no Mongo, então a soma não pode ser feita via aggregation pipeline ($group/$sum);
        // os documentos precisam ser carregados e descriptografados antes de somar.
        var filter = Builders<Entry>.Filter.Eq(x => x.UserId, userId) & Builders<Entry>.Filter.Lt(x => x.Date, before);
        var entries = await _context.Entries.Find(filter).ToListAsync();

        return entries.Sum(e => e.Kind == EntryKind.Entrada ? e.Value : -e.Value);
    }

    public async Task<Entry> CreateAsync(Entry entry)
    {
        await _context.Entries.InsertOneAsync(entry);
        Console.WriteLine($"[DIAG CreateAsync] userId={entry.UserId} id={entry.Id} date={entry.Date:O} kind={entry.Kind}");
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
        // Value é criptografado no Mongo, então o operador $mul não pode atuar direto no banco;
        // os documentos precisam ser carregados, recalculados e regravados em lote.
        var filter = Builders<Entry>.Filter.Eq(x => x.UserId, userId);
        var entries = await _context.Entries.Find(filter).ToListAsync();
        if (entries.Count == 0) return;

        var writes = entries.Select(entry =>
        {
            entry.Value = Math.Round(entry.Value * factor, 2);
            return new ReplaceOneModel<Entry>(Builders<Entry>.Filter.Eq(x => x.Id, entry.Id), entry);
        });

        await _context.Entries.BulkWriteAsync(writes);
    }
}
