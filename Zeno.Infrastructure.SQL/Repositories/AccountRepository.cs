using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ZenoDbContext _context;

    public AccountRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Name, Bank, Type, Balance, WalletId, CreatedAt
                             FROM Accounts WHERE Id = @Id";

        return await _context.Connection.QueryFirstOrDefaultAsync<Account>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Account>> GetByWalletIdAsync(Guid walletId)
    {
        const string sql = @"SELECT Id, Name, Bank, Type, Balance, WalletId, CreatedAt
                             FROM Accounts WHERE WalletId = @WalletId ORDER BY CreatedAt DESC";

        return await _context.Connection.QueryAsync<Account>(sql, new { WalletId = walletId });
    }

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"SELECT a.Id, a.Name, a.Bank, a.Type, a.Balance, a.WalletId, a.CreatedAt
                             FROM Accounts a
                             INNER JOIN Wallets w ON w.Id = a.WalletId
                             WHERE w.UserId = @UserId
                             ORDER BY a.CreatedAt DESC";

        return await _context.Connection.QueryAsync<Account>(sql, new { UserId = userId });
    }

    public async Task<Account> CreateAsync(Account account)
    {
        const string sql = @"INSERT INTO Accounts (Id, Name, Bank, Type, Balance, WalletId, CreatedAt)
                             VALUES (@Id, @Name, @Bank, @Type, @Balance, @WalletId, @CreatedAt)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            account.Id,
            account.Name,
            account.Bank,
            account.Type,
            account.Balance,
            account.WalletId,
            account.CreatedAt
        });

        return account;
    }

    public async Task<Account> UpdateAsync(Account account)
    {
        const string sql = @"UPDATE Accounts
                             SET Name = @Name, Bank = @Bank, Type = @Type
                             WHERE Id = @Id";

        await _context.Connection.ExecuteAsync(sql, new
        {
            account.Id,
            account.Name,
            account.Bank,
            account.Type
        });

        return account;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM Accounts WHERE Id = @Id";

        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task UpdateBalanceAsync(Guid id, decimal newBalance)
    {
        const string sql = @"UPDATE Accounts SET Balance = @Balance WHERE Id = @Id";

        await _context.Connection.ExecuteAsync(sql, new { Id = id, Balance = newBalance });
    }
}