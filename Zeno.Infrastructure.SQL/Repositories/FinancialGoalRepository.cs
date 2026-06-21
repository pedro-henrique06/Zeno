using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.FinancialGoal;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class FinancialGoalRepository : IFinancialGoalRepository
{
    private readonly ZenoMongoContext _context;

    public FinancialGoalRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<FinancialGoal?> GetByIdAsync(Guid id)
    {
        return await _context.FinancialGoals.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<FinancialGoal>> GetByUserAsync(Guid userId)
    {
        return await _context.FinancialGoals
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<FinancialGoal> CreateAsync(FinancialGoal goal)
    {
        await _context.FinancialGoals.InsertOneAsync(goal);
        return goal;
    }

    public async Task<FinancialGoal> UpdateAsync(FinancialGoal goal)
    {
        var filter = Builders<FinancialGoal>.Filter.Eq(x => x.Id, goal.Id);
        await _context.FinancialGoals.ReplaceOneAsync(filter, goal);
        return goal;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.FinancialGoals.DeleteOneAsync(x => x.Id == id);
    }
}
