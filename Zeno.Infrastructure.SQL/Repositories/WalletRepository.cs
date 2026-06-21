using MongoDB.Driver;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ZenoMongoContext _context;

    public WalletRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        return await _context.Wallets.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Wallet?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        return await _context.Wallets.Find(x => x.Id == id && x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Wallet>> GetAllByUserAsync(Guid userId)
    {
        return await _context.Wallets
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Wallets
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalBalanceByUserAsync(Guid userId)
    {
        var wallets = await _context.Wallets.Find(x => x.UserId == userId).ToListAsync();
        return wallets.Sum(w => w.Balance);
    }

    public async Task<decimal> GetTotalByUserAndMonthAsync(Guid userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var entries = await _context.Entries.Find(x =>
            x.Date >= startDate &&
            x.Date < endDate &&
            x.Type == Zeno.Domain.Enum.EntryType.Debit).ToListAsync();

        var walletIds = await _context.Wallets.Find(x => x.UserId == userId).Project(x => x.Id).ToListAsync();
        
        return entries
            .Where(e => walletIds.Contains(e.WalletId))
            .Sum(e => e.Value);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet)
    {
        await _context.Wallets.InsertOneAsync(wallet);
        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet)
    {
        var filter = Builders<Wallet>.Filter.Eq(x => x.Id, wallet.Id);
        await _context.Wallets.ReplaceOneAsync(filter, wallet);
        return wallet;
    }

    public async Task DeleteAsync(Guid id)
    {
        var filter = Builders<Wallet>.Filter.Eq(x => x.Id, id);
        await _context.Wallets.DeleteOneAsync(filter);
    }

    public async Task AddBalanceAsync(Guid id, decimal amount, object? transaction = null)
    {
        var filter = Builders<Wallet>.Filter.Eq(x => x.Id, id);
        var update = Builders<Wallet>.Update.Inc(x => x.Balance, amount);
        await _context.Wallets.UpdateOneAsync(filter, update);
    }

    public async Task UpdateBudgetAsync(Guid id, decimal? dailyBudget)
    {
        var filter = Builders<Wallet>.Filter.Eq(x => x.Id, id);
        var update = Builders<Wallet>.Update.Set(x => x.DailyBudget, dailyBudget);
        await _context.Wallets.UpdateOneAsync(filter, update);
    }
}
