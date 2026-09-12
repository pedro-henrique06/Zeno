using Zeno.Application.Requests.MonthlyExpenseCategories;
using MonthlyExpenseCategoryEntity = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;

namespace Zeno.Application.Interfaces;

public interface IMonthlyExpenseCategoryService
{
    Task<IEnumerable<MonthlyExpenseCategoryEntity>> GetAllAsync(Guid userId);
    Task<MonthlyExpenseCategoryEntity?> GetByIdAsync(Guid userId, Guid id);
    Task<MonthlyExpenseCategoryEntity> CreateAsync(Guid userId, CreateMonthlyExpenseCategoryRequest request);
    Task UpdateAsync(Guid userId, UpdateMonthlyExpenseCategoryRequest request);
    Task DeleteAsync(Guid userId, Guid id);
}
