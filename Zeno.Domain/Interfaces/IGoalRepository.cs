using Zeno.Domain.Goals;

namespace Zeno.Domain.Interfaces;

public interface IGoalRepository
{
    Task<Goal?> GetByUserAsync(Guid userId);
    Task<Goal> UpsertAsync(Goal goal);
    Task DeleteByUserAsync(Guid userId);
}
