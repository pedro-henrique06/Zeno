using Zeno.Application.Requests;
using Zeno.Domain.Interfaces;
using Zeno.Domain.RecurringExpense;

namespace Zeno.Application.Interfaces;

public interface IRecurringExpenseService
{
    Task<RecurringExpense> CreateAsync(Guid userId, CreateRecurringExpenseRequest request);
    Task<IEnumerable<RecurringExpense>> GetAllAsync(Guid userId);
    Task<RecurringExpense?> GetByIdAsync(Guid userId, Guid id);
    Task<RecurringExpense> UpdateAsync(Guid userId, UpdateRecurringExpenseRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task ProcessMonthlyAsync();
}