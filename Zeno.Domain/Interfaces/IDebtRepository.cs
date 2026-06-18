using DebtEntity = Zeno.Domain.Debt.Debt;

namespace Zeno.Domain.Interfaces;

public interface IDebtRepository
{
    Task<DebtEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<DebtEntity>> GetByUserAsync(Guid userId);
    Task<DebtEntity> CreateAsync(DebtEntity debt);
    Task<DebtEntity> UpdateAsync(DebtEntity debt);
    Task DeleteAsync(Guid id);
}