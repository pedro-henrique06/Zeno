using MongoDB.Driver;
using Zeno.Domain.Goals;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class GoalRepository : IGoalRepository
{
    private readonly ZenoMongoContext _context;

    public GoalRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<Goal?> GetByUserAsync(Guid userId)
    {
        return await _context.Goals.Find(x => x.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<Goal> UpsertAsync(Goal goal)
    {
        var filter = Builders<Goal>.Filter.Eq(x => x.UserId, goal.UserId);
        await _context.Goals.ReplaceOneAsync(filter, goal, new ReplaceOptions { IsUpsert = true });
        return goal;
    }

    public async Task DeleteByUserAsync(Guid userId)
    {
        await _context.Goals.DeleteOneAsync(x => x.UserId == userId);
    }
}
