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
        const string sql = @"SELECT Id, UserId, AccountId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt
                             FROM Salaries WHERE Id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToSalary(row);
    }

    public async Task<Salary?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT s.Id, s.AccountId, s.Amount, s.Description, s.DayOfMonth, s.Active, s.CreatedAt, s.LastProcessedAt
                             FROM Salaries s
                             INNER JOIN Accounts a ON s.AccountId = a.Id
                             INNER JOIN Wallets w ON a.WalletId = w.Id
                             WHERE s.Id = @Id AND w.UserId = @UserId";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });
        return row is null ? null : MapToSalary(row);
    }

    public async Task<IEnumerable<Salary>> GetByAccountAsync(Guid accountId)
    {
        const string sql = @"SELECT Id, UserId, AccountId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt
                             FROM Salaries WHERE AccountId = @AccountId ORDER BY DayOfMonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { AccountId = accountId });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<IEnumerable<Salary>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT s.Id, s.AccountId, s.Amount, s.Description, s.DayOfMonth, s.Active, s.CreatedAt, s.LastProcessedAt
                             FROM Salaries s
                             INNER JOIN Accounts a ON s.AccountId = a.Id
                             INNER JOIN Wallets w ON a.WalletId = w.Id
                             WHERE w.UserId = @UserId
                             ORDER BY s.DayOfMonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<IEnumerable<Salary>> GetPendingSalariesAsync(int dayOfMonth)
    {
        const string sql = @"SELECT Id, UserId, AccountId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt
                             FROM Salaries
                             WHERE Active = true AND DayOfMonth = @DayOfMonth
                             AND (LastProcessedAt IS NULL OR EXTRACT(MONTH FROM LastProcessedAt) != EXTRACT(MONTH FROM NOW()) OR EXTRACT(YEAR FROM LastProcessedAt) != EXTRACT(YEAR FROM NOW()))";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { DayOfMonth = dayOfMonth });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<Salary> CreateAsync(Salary salary)
    {
        const string sql = @"INSERT INTO Salaries (Id, UserId, AccountId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt)
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
        const string sql = @"UPDATE Salaries
                            SET Amount = @Amount, Description = @Description, DayOfMonth = @DayOfMonth, Active = @Active, AccountId = @AccountId
                            WHERE Id = @Id";
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
        const string sql = @"DELETE FROM Salaries WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task MarkProcessedAsync(Guid id)
    {
        const string sql = @"UPDATE Salaries SET LastProcessedAt = NOW() WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private Salary MapToSalary(dynamic row)
    {
        var amount = row.Amount is string s
            ? _encryption.DecryptDecimal(s)
            : (decimal)row.Amount;
        return new Salary
        {
            Id = row.Id,
            UserId = row.UserId,
            AccountId = row.AccountId,
            Amount = amount,
            Description = row.Description,
            DayOfMonth = row.DayOfMonth,
            Active = row.Active,
            CreatedAt = row.CreatedAt,
            LastProcessedAt = row.LastProcessedAt
        };
    }
}