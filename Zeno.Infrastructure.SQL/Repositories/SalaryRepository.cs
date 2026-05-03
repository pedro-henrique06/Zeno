using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Salary;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class SalaryRepository : ISalaryRepository
{
    private readonly ZenoDbContext _context;

    public SalaryRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Salary?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, accountid, amount, description, dayofmonth, active, createdat, lastprocessedat
                             FROM salaries WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToSalary(row);
    }

    public async Task<Salary?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT s.id, s.accountid, s.amount, s.description, s.dayofmonth, s.active, s.createdat, s.lastprocessedat
                             FROM salaries s
                             INNER JOIN accounts a ON s.accountid = a.id
                             INNER JOIN wallets w ON a.wallet_id = w.id
                             WHERE s.id = @Id AND w.userid = @UserId";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });
        return row is null ? null : MapToSalary(row);
    }

    public async Task<IEnumerable<Salary>> GetByAccountAsync(Guid accountId)
    {
        const string sql = @"SELECT id, userid, accountid, amount, description, dayofmonth, active, createdat, lastprocessedat
                             FROM salaries WHERE accountid = @AccountId ORDER BY dayofmonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { AccountId = accountId });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<IEnumerable<Salary>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT s.id, s.accountid, s.amount, s.description, s.dayofmonth, s.active, s.createdat, s.lastprocessedat
                             FROM salaries s
                             INNER JOIN accounts a ON s.accountid = a.id
                             INNER JOIN wallets w ON a.wallet_id = w.id
                             WHERE w.userid = @UserId
                             ORDER BY s.dayofmonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<IEnumerable<Salary>> GetPendingSalariesAsync(int dayOfMonth)
    {
        const string sql = @"SELECT id, userid, accountid, amount, description, dayofmonth, active, createdat, lastprocessedat
                             FROM salaries
                             WHERE active = true AND dayofmonth = @DayOfMonth
                             AND (lastprocessedat IS NULL OR EXTRACT(MONTH FROM lastprocessedat) != EXTRACT(MONTH FROM NOW()) OR EXTRACT(YEAR FROM lastprocessedat) != EXTRACT(YEAR FROM NOW()))";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { DayOfMonth = dayOfMonth });
        return rows.Select(r => MapToSalary(r)).Cast<Salary>();
    }

    public async Task<Salary> CreateAsync(Salary salary)
    {
        const string sql = @"INSERT INTO salaries (id, userid, accountid, amount, description, dayofmonth, active, createdat, lastprocessedat)
                             VALUES (@Id, @UserId, @AccountId, @Amount, @Description, @DayOfMonth, @Active, @CreatedAt, @LastProcessedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            salary.Id,
            salary.UserId,
            salary.AccountId,
            salary.Amount,
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
                             SET amount = @Amount, description = @Description, dayofmonth = @DayOfMonth, active = @Active, accountid = @AccountId
                             WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            salary.Id,
            salary.AccountId,
            salary.Amount,
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
        const string sql = @"UPDATE salaries SET lastprocessedat = NOW() WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private Salary MapToSalary(dynamic row)
    {
        return new Salary
        {
            Id = row.id,
            UserId = row.userid,
            AccountId = row.accountid,
            Amount = (decimal)row.amount,
            Description = row.description,
            DayOfMonth = row.dayofmonth,
            Active = row.active,
            CreatedAt = row.createdat,
            LastProcessedAt = row.lastprocessedat
        };
    }
}