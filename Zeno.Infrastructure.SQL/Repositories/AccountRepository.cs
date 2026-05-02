using System.Data;
using Dapper;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ZenoDbContext _context;
    private readonly IEncryptionService _encryption;

    public AccountRepository(ZenoDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, name, bank, type, balance, wallet_id, createdat
                             FROM accounts WHERE id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToAccount(row);
    }

    public async Task<IEnumerable<Account>> GetByWalletIdAsync(Guid walletId)
    {
        const string sql = @"SELECT id, name, bank, type, balance, wallet_id, createdat
                             FROM accounts WHERE wallet_id = @WalletId ORDER BY createdat DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { WalletId = walletId });
        return rows.Select(r => MapToAccount(r)).Cast<Account>();
    }

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"SELECT a.id, a.name, a.bank, a.type, a.balance, a.wallet_id, a.createdat
                             FROM accounts a
                             INNER JOIN wallets w ON w.id = a.wallet_id
                             WHERE w.user_id = @UserId
                             ORDER BY a.createdat DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToAccount(r)).Cast<Account>();
    }

    public async Task<Account> CreateAsync(Account account)
    {
        const string sql = @"INSERT INTO accounts (id, name, bank, type, balance, wallet_id, createdat)
                             VALUES (@Id, @Name, @Bank, @Type, @Balance, @WalletId, @CreatedAt)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            account.Id,
            account.Name,
            account.Bank,
            account.Type,
            Balance = _encryption.EncryptDecimal(account.Balance),
            account.WalletId,
            account.CreatedAt
        });

        return account;
    }

    public async Task<Account> UpdateAsync(Account account)
    {
        const string sql = @"UPDATE accounts
                             SET name = @Name, bank = @Bank, type = @Type
                             WHERE id = @Id";

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
        const string sql = @"DELETE FROM accounts WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task UpdateBalanceAsync(Guid id, decimal newBalance)
    {
        const string sql = @"UPDATE accounts SET balance = @Balance WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id, Balance = _encryption.EncryptDecimal(newBalance) });
    }

    private Account MapToAccount(dynamic row)
    {
        var balance = row.balance is string s
            ? _encryption.DecryptDecimal(s)
            : (decimal)row.balance;
        return new Account
        {
            Id = row.id,
            Name = row.name,
            Bank = row.bank,
            Type = row.type,
            Balance = balance,
            WalletId = row.wallet_id,
            CreatedAt = row.createdat
        };
    }
}