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

    public async Task<IEnumerable<Entry>> GetByMonthAsync(int month, int year, Guid walletId)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return await _context.Entries
            .Find(x => x.Date >= startDate && x.Date < endDate && x.WalletId == walletId)
            .SortByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Entry> Items, int TotalCount)> GetByMonthPagedAsync(
        int month, int year, Guid walletId, EntryType? type, Category? category, int page, int pageSize)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var builder = Builders<Entry>.Filter;
        var filter = builder.Gte(x => x.Date, startDate) & builder.Lt(x => x.Date, endDate) & builder.Eq(x => x.WalletId, walletId);

        if (type.HasValue)
            filter &= builder.Eq(x => x.Type, type.Value);
        if (category.HasValue)
            filter &= builder.Eq(x => x.Category, category.Value);

        var totalCount = await _context.Entries.CountDocumentsAsync(filter);

        var items = await _context.Entries
            .Find(filter)
            .SortByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, (int)totalCount);
    }

    public async Task<(IEnumerable<Entry> Items, int TotalCount)> GetByMonthForUserPagedAsync(int month, int year, Guid userId, int page, int pageSize)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        // Get wallet IDs for the user
        var walletIds = await _context.Wallets
            .Find(x => x.UserId == userId)
            .Project(x => x.Id)
            .ToListAsync();

        var builder = Builders<Entry>.Filter;
        var filter = builder.Gte(x => x.Date, startDate) & 
                     builder.Lt(x => x.Date, endDate) & 
                     builder.In(x => x.WalletId, walletIds);

        var totalCount = await _context.Entries.CountDocumentsAsync(filter);

        var items = await _context.Entries
            .Find(filter)
            .SortByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, (int)totalCount);
    }

    public async Task<IEnumerable<Entry>> GetFromDateAsync(Guid walletId, DateTime fromDate)
    {
        return await _context.Entries
            .Find(x => x.WalletId == walletId && x.Date >= fromDate)
            .SortByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Entry>> GetFromDateForUserAsync(Guid userId, DateTime fromDate)
    {
        // Get wallet IDs for the user
        var walletIds = await _context.Wallets
            .Find(x => x.UserId == userId)
            .Project(x => x.Id)
            .ToListAsync();

        return await _context.Entries
            .Find(x => walletIds.Contains(x.WalletId) && x.Date >= fromDate)
            .SortByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Entry>> GetUpToDateAsync(Guid walletId, DateTime toDate)
    {
        return await _context.Entries
            .Find(x => x.WalletId == walletId && x.Date <= toDate)
            .SortBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Entry>> GetUpToDateForUserAsync(Guid userId, DateTime toDate)
    {
        // Get wallet IDs for the user
        var walletIds = await _context.Wallets
            .Find(x => x.UserId == userId)
            .Project(x => x.Id)
            .ToListAsync();

        return await _context.Entries
            .Find(x => walletIds.Contains(x.WalletId) && x.Date <= toDate)
            .SortBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetSumByKindAsync(Guid walletId, EntryKind kind, int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var entries = await _context.Entries
            .Find(x => x.WalletId == walletId && 
                       x.Kind == kind && 
                       x.Date >= startDate && 
                       x.Date < endDate)
            .ToListAsync();

        return entries.Sum(x => x.Value);
    }

    public async Task<decimal> GetTotalByTypeAndWalletAsync(int month, int year, Guid walletId, int type)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var entries = await _context.Entries
            .Find(x => x.Date >= startDate && 
                       x.Date < endDate && 
                       x.WalletId == walletId && 
                       x.Type == (EntryType)type)
            .ToListAsync();

        return entries.Sum(x => x.Value);
    }

    public async Task<IEnumerable<(int Category, decimal Total)>> GetCategoryTotalsAsync(int month, int year, Guid walletId)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var entries = await _context.Entries
            .Find(x => x.Date >= startDate && 
                       x.Date < endDate && 
                       x.WalletId == walletId && 
                       x.Type == EntryType.Debit)
            .ToListAsync();

        return entries
            .GroupBy(x => (int)x.Category)
            .Select(g => ((int)g.Key, g.Sum(x => x.Value)))
            .OrderByDescending(x => x.Item2);
    }

    public async Task<Entry> CreateAsync(Entry entry, object? transaction = null)
    {
        await _context.Entries.InsertOneAsync(entry);
        return entry;
    }

    public async Task<Entry> UpdateAsync(Entry entry, object? transaction = null)
    {
        var filter = Builders<Entry>.Filter.Eq(x => x.Id, entry.Id);
        await _context.Entries.ReplaceOneAsync(filter, entry);
        return entry;
    }

    public async Task DeleteAsync(Guid id, object? transaction = null)
    {
        var filter = Builders<Entry>.Filter.Eq(x => x.Id, id);
        await _context.Entries.DeleteOneAsync(filter);
    }
}
