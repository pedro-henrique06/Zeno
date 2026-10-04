using Zeno.Application.Requests.Goals;
using Zeno.Application.Responses.Goals;

namespace Zeno.Application.Interfaces;

public interface IGoalService
{
    Task<GoalResponse?> GetAsync(Guid userId);
    Task<GoalResponse> SaveAsync(Guid userId, SaveGoalRequest request);
    Task DeleteAsync(Guid userId);
}
