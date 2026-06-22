using MonthlyExpenseCategoryEntity = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;

namespace Zeno.Domain.Interfaces;

public interface IMonthlyExpenseCategoryRepository
{
    Task<MonthlyExpenseCategoryEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<MonthlyExpenseCategoryEntity>> GetByUserAsync(Guid userId);
    Task<MonthlyExpenseCategoryEntity> CreateAsync(MonthlyExpenseCategoryEntity category);
    Task<MonthlyExpenseCategoryEntity> UpdateAsync(MonthlyExpenseCategoryEntity category);
    Task DeleteAsync(Guid id);
    Task MultiplyAmountsForUserAsync(Guid userId, decimal factor);
}
