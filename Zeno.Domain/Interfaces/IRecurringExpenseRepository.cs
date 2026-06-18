using RecurringExpenseEntity = Zeno.Domain.RecurringExpense.RecurringExpense;

namespace Zeno.Domain.Interfaces;

public interface IRecurringExpenseRepository
{
    Task<RecurringExpenseEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<RecurringExpenseEntity>> GetByUserAsync(Guid userId);
    Task<IEnumerable<RecurringExpenseEntity>> GetActiveByDayAsync(int dayOfMonth);
    Task<RecurringExpenseEntity> CreateAsync(RecurringExpenseEntity expense);
    Task<RecurringExpenseEntity> UpdateAsync(RecurringExpenseEntity expense);
    Task DeleteAsync(Guid id);
}