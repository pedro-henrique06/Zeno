using System.Data;
using Dapper;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ZenoDbContext _context;

    public WalletRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, name, description, balance, userid, currency, dailybudget, createdat
                             FROM wallets WHERE id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToWallet(row);
    }

    public async Task<Wallet?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT id, name, description, balance, userid, currency, dailybudget, createdat
                             FROM wallets WHERE id = @Id AND userid = @UserId";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });
        return row is null ? null : MapToWallet(row);
    }

    public async Task<IEnumerable<Wallet>> GetAllByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, name, description, balance, userid, currency, dailybudget, createdat
                             FROM wallets WHERE userid = @UserId ORDER BY createdat DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToWallet(r)).Cast<Wallet>();
    }

    public async Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"SELECT id, name, description, balance, userid, currency, dailybudget, createdat
                             FROM wallets WHERE userid = @UserId ORDER BY createdat DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToWallet(r)).Cast<Wallet>();
    }

    public async Task<decimal> GetTotalBalanceByUserAsync(Guid userId)
    {
        var sql = @"SELECT COALESCE(SUM(balance), 0) FROM wallets WHERE userid = @UserId";
        return await _context.Connection.ExecuteScalarAsync<decimal>(sql, new { UserId = userId });
    }

    public async Task<decimal> GetTotalByUserAndMonthAsync(Guid userId, int month, int year)
    {
        var sql = @"SELECT COALESCE(SUM(e.value), 0)
                    FROM entries e
                    INNER JOIN wallets w ON e.walletid = w.id
                    WHERE w.userid = @UserId
                      AND EXTRACT(MONTH FROM e.date) = @Month
                      AND EXTRACT(YEAR FROM e.date) = @Year
                      AND e.type = 0";

        return await _context.Connection.ExecuteScalarAsync<decimal>(sql, new { UserId = userId, Month = month, Year = year });
    }

    public async Task<Wallet> CreateAsync(Wallet wallet)
    {
        const string sql = @"INSERT INTO wallets (id, name, description, balance, userid, currency, dailybudget, createdat)
                             VALUES (@Id, @Name, @Description, @Balance, @UserId, @Currency, @DailyBudget, @CreatedAt)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.Name,
            wallet.Description,
            wallet.Balance,
            UserId = wallet.UserId,
            wallet.Currency,
            wallet.DailyBudget,
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

    public async Task AddBalanceAsync(Guid id, decimal amount, object? transaction = null)
    {
        const string sql = @"UPDATE wallets SET balance = balance + @Amount WHERE id = @Id";
        var tx = transaction as IDbTransaction;
        await _context.Connection.ExecuteAsync(sql, new { Id = id, Amount = amount }, tx);
    }

    public async Task UpdateBudgetAsync(Guid id, decimal? dailyBudget)
    {
        const string sql = @"UPDATE wallets SET dailybudget = @DailyBudget WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id, DailyBudget = dailyBudget });
    }

    private Wallet MapToWallet(dynamic row)
    {
        return new Wallet
        {
            Id = row.id,
            Name = row.name,
            Description = row.description,
            UserId = row.userid,
            Balance = (decimal)row.balance,
            Currency = row.currency,
            DailyBudget = row.dailybudget is null ? null : (decimal)row.dailybudget,
            CreatedAt = row.createdat
        };
    }
}