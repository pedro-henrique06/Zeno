using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Domain.Interfaces;
using Zeno.Domain.FinancialGoal;

namespace Zeno.Application.Interfaces;

public interface IFinancialGoalService
{
    Task<FinancialGoal> CreateAsync(Guid userId, CreateFinancialGoalRequest request);
    Task<IEnumerable<FinancialGoal>> GetAllAsync(Guid userId);
    Task<FinancialGoal?> GetByIdAsync(Guid userId, Guid id);
    Task<FinancialGoal> UpdateAsync(Guid userId, UpdateFinancialGoalRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task<GoalSimulationResponse> GetSimulationAsync(Guid userId, Guid id);
}