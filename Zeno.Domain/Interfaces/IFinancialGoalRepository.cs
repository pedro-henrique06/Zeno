using FinancialGoalEntity = Zeno.Domain.FinancialGoal.FinancialGoal;

namespace Zeno.Domain.Interfaces;

public interface IFinancialGoalRepository
{
    Task<FinancialGoalEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<FinancialGoalEntity>> GetByUserAsync(Guid userId);
    Task<FinancialGoalEntity> CreateAsync(FinancialGoalEntity goal);
    Task<FinancialGoalEntity> UpdateAsync(FinancialGoalEntity goal);
    Task DeleteAsync(Guid id);
}