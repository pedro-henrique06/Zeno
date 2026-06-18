using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.FinancialGoal;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class FinancialGoalRepository : IFinancialGoalRepository
{
    private readonly ZenoDbContext _context;

    public FinancialGoalRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialGoal?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, name, targetamount, currentamount, targetdate, createdat
                             FROM financial_goals WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToFinancialGoal(row);
    }

    public async Task<IEnumerable<FinancialGoal>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, userid, name, targetamount, currentamount, targetdate, createdat
                             FROM financial_goals WHERE userid = @UserId ORDER BY createdat DESC";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToFinancialGoal(r)).Cast<FinancialGoal>();
    }

    public async Task<FinancialGoal> CreateAsync(FinancialGoal goal)
    {
        const string sql = @"INSERT INTO financial_goals (id, userid, name, targetamount, currentamount, targetdate, createdat)
                             VALUES (@Id, @UserId, @Name, @TargetAmount, @CurrentAmount, @TargetDate, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            goal.Id,
            goal.UserId,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentAmount,
            goal.TargetDate,
            goal.CreatedAt
        });
        return goal;
    }

    public async Task<FinancialGoal> UpdateAsync(FinancialGoal goal)
    {
        const string sql = @"UPDATE financial_goals
                             SET name = @Name, targetamount = @TargetAmount, currentamount = @CurrentAmount, targetdate = @TargetDate
                             WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentAmount,
            goal.TargetDate
        });
        return goal;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM financial_goals WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private FinancialGoal MapToFinancialGoal(dynamic row)
    {
        return new FinancialGoal
        {
            Id = row.id,
            UserId = row.userid,
            Name = row.name,
            TargetAmount = (decimal)row.targetamount,
            CurrentAmount = (decimal)row.currentamount,
            TargetDate = DateOnly.FromDateTime(row.targetdate),
            CreatedAt = row.createdat
        };
    }
}