using Dapper;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Salary;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class SalaryRepository : ISalaryRepository
{
    private readonly ZenoDbContext _context;
    private readonly IEncryptionService _encryption;

    public SalaryRepository(ZenoDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<Salary?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, user_id, account_id, amount, description, day_of_month, active, created_at, last_processed_at
                             FROM salaries WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToSalary(row);
    }

    public async Task<Salary?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT s.id, s.account_id, s.amount, s.description, s.day_of_month, s.active, s.created_at, s.last_processed_at
                             FROM salaries s
                             INNER JOIN accounts a ON s.account_id = a.id
                             INNER JOIN wallets w ON a.wallet_id = w.id
                             WHERE s.id = @Id AND w.user_id = @UserId";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });
        return row is null ? null : MapToSalary(row);
    }

    public async Task<IEnumerable<Salary>> GetByAccountAsync(Guid accountId)
    {
        const string sql = @"SELECT id, user_id, account_id, amount, description, day_of_month, active, created_at, last_processed_at
                             FROM salaries WHERE account_id = @AccountId ORDER BY day_of_month";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { AccountId = accountId });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<IEnumerable<Salary>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT s.id, s.account_id, s.amount, s.description, s.day_of_month, s.active, s.created_at, s.last_processed_at
                             FROM salaries s
                             INNER JOIN accounts a ON s.account_id = a.id
                             INNER JOIN wallets w ON a.wallet_id = w.id
                             WHERE w.user_id = @UserId
                             ORDER BY s.day_of_month";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<IEnumerable<Salary>> GetPendingSalariesAsync(int dayOfMonth)
    {
        const string sql = @"SELECT id, user_id, account_id, amount, description, day_of_month, active, created_at, last_processed_at
                             FROM salaries
                             WHERE active = true AND day_of_month = @DayOfMonth
                             AND (last_processed_at IS NULL OR EXTRACT(MONTH FROM last_processed_at) != EXTRACT(MONTH FROM NOW()) OR EXTRACT(YEAR FROM last_processed_at) != EXTRACT(YEAR FROM NOW()))";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { DayOfMonth = dayOfMonth });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<Salary> CreateAsync(Salary salary)
    {
        const string sql = @"INSERT INTO salaries (id, user_id, account_id, amount, description, day_of_month, active, created_at, last_processed_at)
                             VALUES (@Id, @UserId, @AccountId, @Amount, @Description, @DayOfMonth, @Active, @CreatedAt, @LastProcessedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            salary.Id,
            salary.UserId,
            salary.AccountId,
            Amount = _encryption.EncryptDecimal(salary.Amount),
            salary.Description,
            salary.DayOfMonth,
            salary.Active,
            salary.CreatedAt,
            salary.LastProcessedAt
        });
        return salary;
    }

    public async Task<Salary> UpdateAsync(Salary salary)
    {
        const string sql = @"UPDATE salaries
                             SET amount = @Amount, description = @Description, day_of_month = @DayOfMonth, active = @Active, account_id = @AccountId
                             WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            salary.Id,
            salary.AccountId,
            Amount = _encryption.EncryptDecimal(salary.Amount),
            salary.Description,
            salary.DayOfMonth,
            salary.Active
        });
        return salary;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM salaries WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task MarkProcessedAsync(Guid id)
    {
        const string sql = @"UPDATE salaries SET last_processed_at = NOW() WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private Salary MapToSalary(dynamic row)
    {
        var amount = row.amount is string s
            ? _encryption.DecryptDecimal(s)
            : (decimal)row.amount;
        return new Salary
        {
            Id = row.id,
            UserId = row.user_id,
            AccountId = row.account_id,
            Amount = amount,
            Description = row.description,
            DayOfMonth = row.day_of_month,
            Active = row.active,
            CreatedAt = row.created_at,
            LastProcessedAt = row.last_processed_at
        };
    }
}