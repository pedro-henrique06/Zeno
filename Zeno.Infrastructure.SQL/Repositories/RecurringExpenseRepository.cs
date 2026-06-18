using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.RecurringExpense;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class RecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly ZenoDbContext _context;

    public RecurringExpenseRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<RecurringExpense?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurring_expenses WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToRecurringExpense(row);
    }

    public async Task<IEnumerable<RecurringExpense>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurring_expenses WHERE userid = @UserId ORDER BY createdat DESC";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToRecurringExpense(r)).Cast<RecurringExpense>();
    }

    public async Task<IEnumerable<RecurringExpense>> GetActiveByDayAsync(int dayOfMonth)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurring_expenses WHERE isactive = true AND dayofmonth = @DayOfMonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { DayOfMonth = dayOfMonth });
        return rows.Select(r => MapToRecurringExpense(r)).Cast<RecurringExpense>();
    }

    public async Task<RecurringExpense> CreateAsync(RecurringExpense expense)
    {
        const string sql = @"INSERT INTO recurring_expenses (id, userid, walletid, title, value, dayofmonth, isactive, createdat, lastprocessedat)
                             VALUES (@Id, @UserId, @WalletId, @Title, @Value, @DayOfMonth, @IsActive, @CreatedAt, @LastProcessedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            expense.Id,
            expense.UserId,
            expense.WalletId,
            expense.Title,
            expense.Value,
            expense.DayOfMonth,
            expense.IsActive,
            expense.CreatedAt,
            expense.LastProcessedAt
        });
        return expense;
    }

    public async Task<RecurringExpense> UpdateAsync(RecurringExpense expense)
    {
        const string sql = @"UPDATE recurring_expenses
                             SET title = @Title, value = @Value, dayofmonth = @DayOfMonth, isactive = @IsActive, lastprocessedat = @LastProcessedAt
                             WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            expense.Id,
            expense.Title,
            expense.Value,
            expense.DayOfMonth,
            expense.IsActive,
            expense.LastProcessedAt
        });
        return expense;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM recurring_expenses WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private RecurringExpense MapToRecurringExpense(dynamic row)
    {
        return new RecurringExpense
        {
            Id = row.id,
            UserId = row.userid,
            WalletId = row.walletid,
            Title = row.title,
            Value = (decimal)row.value,
            DayOfMonth = (int)row.dayofmonth,
            IsActive = row.isactive,
            CreatedAt = row.createdat,
            LastProcessedAt = row.lastprocessedat
        };
    }
}