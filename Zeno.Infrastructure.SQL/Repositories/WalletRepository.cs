using Dapper;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ZenoDbContext _context;
    private readonly IEncryptionService _encryption;

    public WalletRepository(ZenoDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, name, description, balance, user_id, currency, createdat
                             FROM wallets WHERE id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToWallet(row);
    }

    public async Task<Wallet?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT id, name, description, balance, user_id, currency, createdat
                             FROM wallets WHERE id = @Id AND user_id = @UserId";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });
        return row is null ? null : MapToWallet(row);
    }

    public async Task<IEnumerable<Wallet>> GetAllByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, name, description, balance, user_id, currency, createdat
                             FROM wallets WHERE user_id = @UserId ORDER BY createdat DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToWallet(r)).Cast<Wallet>();
    }

    public async Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"SELECT id, name, description, balance, user_id, currency, createdat
                             FROM wallets WHERE user_id = @UserId ORDER BY createdat DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToWallet(r)).Cast<Wallet>();
    }

    public async Task<Wallet> CreateAsync(Wallet wallet)
    {
        const string sql = @"INSERT INTO wallets (id, name, description, balance, user_id, currency, createdat)
                             VALUES (@Id, @Name, @Description, @Balance, @UserId, @Currency, @CreatedAt)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.UserId,
            Balance = _encryption.EncryptDecimal(wallet.Balance),
            wallet.Currency,
            wallet.CreatedAt
        });

        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet)
    {
        const string sql = @"UPDATE wallets
                             SET name = @Name, description = @Description, currency = @Currency
                             WHERE id = @Id";

        await _context.Connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.Currency
        });

        return wallet;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM wallets WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task AddBalanceAsync(Guid id, decimal amount)
    {
        var current = await GetByIdAsync(id);
        if (current is not null)
        {
            var newBalance = current.Balance + amount;
            const string sql = @"UPDATE wallets SET balance = @Balance WHERE id = @Id";
            await _context.Connection.ExecuteAsync(sql, new { Id = id, Balance = _encryption.EncryptDecimal(newBalance) });
        }
    }

    private Wallet MapToWallet(dynamic row)
    {
        var balance = row.balance is string s
            ? _encryption.DecryptDecimal(s)
            : (decimal)row.balance;
        return new Wallet
        {
            Id = row.id,
            Name = row.name,
            Description = row.description,
            UserId = row.user_id,
            Balance = balance,
            Currency = row.currency,
            CreatedAt = row.createdat
        };
    }
}