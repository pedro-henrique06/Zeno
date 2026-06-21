using MongoDB.Driver;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Recurring;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class RecurringEntryRepository : IRecurringEntryRepository
{
    private readonly ZenoMongoContext _context;

    public RecurringEntryRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<RecurringEntry?> GetByIdAsync(Guid id)
    {
        return await _context.RecurrentEntries.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<RecurringEntry?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        return await _context.RecurrentEntries.Find(x => x.Id == id && x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RecurringEntry>> GetByUserAsync(Guid userId)
    {
        return await _context.RecurrentEntries
            .Find(x => x.UserId == userId)
            .SortBy(x => x.DayOfMonth)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecurringEntry>> GetByWalletAsync(Guid walletId)
    {
        return await _context.RecurrentEntries
            .Find(x => x.WalletId == walletId)
            .SortBy(x => x.DayOfMonth)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecurringEntry>> GetActiveByDayAsync(int dayOfMonth)
    {
        return await _context.RecurrentEntries
            .Find(x => x.IsActive == true && x.DayOfMonth == dayOfMonth)
            .ToListAsync();
    }

    public async Task<RecurringEntry> CreateAsync(RecurringEntry entry)
    {
        await _context.RecurrentEntries.InsertOneAsync(entry);
        return entry;
    }

    public async Task<RecurringEntry> UpdateAsync(RecurringEntry entry)
    {
        var filter = Builders<RecurringEntry>.Filter.Eq(x => x.Id, entry.Id);
        await _context.RecurrentEntries.ReplaceOneAsync(filter, entry);
        return entry;
    }

    public async Task DeleteAsync(Guid id)
    {
        var filter = Builders<RecurringEntry>.Filter.Eq(x => x.Id, id);
        await _context.RecurrentEntries.DeleteOneAsync(filter);
    }
}
