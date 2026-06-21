using MongoDB.Driver;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ZenoMongoContext _context;

    public AccountRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Account>> GetByWalletIdAsync(Guid walletId)
    {
        return await _context.Accounts
            .Find(x => x.WalletId == walletId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId)
    {
        var walletIds = await _context.Wallets
            .Find(x => x.UserId == userId)
            .Project(x => x.Id)
            .ToListAsync();

        return await _context.Accounts
            .Find(x => walletIds.Contains(x.WalletId))
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Account> CreateAsync(Account account)
    {
        await _context.Accounts.InsertOneAsync(account);
        return account;
    }

    public async Task<Account> UpdateAsync(Account account)
    {
        var filter = Builders<Account>.Filter.Eq(x => x.Id, account.Id);
        await _context.Accounts.ReplaceOneAsync(filter, account);
        return account;
    }

    public async Task DeleteAsync(Guid id)
    {
        var filter = Builders<Account>.Filter.Eq(x => x.Id, id);
        await _context.Accounts.DeleteOneAsync(filter);
    }

    public async Task UpdateBalanceAsync(Guid id, decimal newBalance)
    {
        var filter = Builders<Account>.Filter.Eq(x => x.Id, id);
        var update = Builders<Account>.Update.Set(x => x.Balance, newBalance);
        await _context.Accounts.UpdateOneAsync(filter, update);
    }
}
