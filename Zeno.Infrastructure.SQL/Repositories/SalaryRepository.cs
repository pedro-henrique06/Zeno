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
        const string sql = @"SELECT Id, WalletId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt 
                             FROM Salaries WHERE Id = @Id";
        return await _context.Connection.QueryFirstOrDefaultAsync<Salary>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Salary>> GetByWalletAsync(Guid walletId)
    {
        const string sql = @"SELECT Id, WalletId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt 
                             FROM Salaries WHERE WalletId = @WalletId ORDER BY DayOfMonth";
        return await _context.Connection.QueryAsync<Salary>(sql, new { WalletId = walletId });
    }

    public async Task<IEnumerable<Salary>> GetPendingSalariesAsync(int dayOfMonth)
    {
        const string sql = @"SELECT Id, WalletId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt 
                             FROM Salaries 
                             WHERE Active = 1 AND DayOfMonth = @DayOfMonth
                             AND (LastProcessedAt IS NULL OR MONTH(LastProcessedAt) != MONTH(GETUTCDATE()) OR YEAR(LastProcessedAt) != YEAR(GETUTCDATE()))";
        return await _context.Connection.QueryAsync<Salary>(sql, new { DayOfMonth = dayOfMonth });
    }

    public async Task<Salary> CreateAsync(Salary salary)
    {
        const string sql = @"INSERT INTO Salaries (Id, WalletId, Amount, Description, DayOfMonth, Active, CreatedAt, LastProcessedAt) 
                             VALUES (@Id, @WalletId, @Amount, @Description, @DayOfMonth, @Active, @CreatedAt, @LastProcessedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            salary.Id,
            salary.WalletId,
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
        const string sql = @"UPDATE Salaries 
                             SET Amount = @Amount, Description = @Description, DayOfMonth = @DayOfMonth, Active = @Active 
                             WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            salary.Id,
            salary.Amount,
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
        const string sql = @"UPDATE Salaries SET LastProcessedAt = GETUTCDATE() WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }
}
