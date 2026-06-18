using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Debt;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class DebtRepository : IDebtRepository
{
    private readonly ZenoDbContext _context;

    public DebtRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Debt?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, name, totalamount, paidamount, monthlypayment, createdat
                             FROM debts WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToDebt(row);
    }

    public async Task<IEnumerable<Debt>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, userid, name, totalamount, paidamount, monthlypayment, createdat
                             FROM debts WHERE userid = @UserId ORDER BY createdat DESC";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToDebt(r)).Cast<Debt>();
    }

    public async Task<Debt> CreateAsync(Debt debt)
    {
        const string sql = @"INSERT INTO debts (id, userid, name, totalamount, paidamount, monthlypayment, createdat)
                             VALUES (@Id, @UserId, @Name, @TotalAmount, @PaidAmount, @MonthlyPayment, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            debt.Id,
            debt.UserId,
            debt.Name,
            debt.TotalAmount,
            debt.PaidAmount,
            debt.MonthlyPayment,
            debt.CreatedAt
        });
        return debt;
    }

    public async Task<Debt> UpdateAsync(Debt debt)
    {
        const string sql = @"UPDATE debts
                             SET name = @Name, totalamount = @TotalAmount, paidamount = @PaidAmount, monthlypayment = @MonthlyPayment
                             WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            debt.Id,
            debt.Name,
            debt.TotalAmount,
            debt.PaidAmount,
            debt.MonthlyPayment
        });
        return debt;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM debts WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private Debt MapToDebt(dynamic row)
    {
        return new Debt
        {
            Id = row.id,
            UserId = row.userid,
            Name = row.name,
            TotalAmount = (decimal)row.totalamount,
            PaidAmount = (decimal)row.paidamount,
            MonthlyPayment = (decimal)row.monthlypayment,
            CreatedAt = row.createdat
        };
    }
}