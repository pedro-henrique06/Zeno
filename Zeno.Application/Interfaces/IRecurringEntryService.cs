using Zeno.Application.Requests;
using Zeno.Domain.Recurring;

namespace Zeno.Application.Interfaces;

public interface IRecurringEntryService
{
    Task<RecurringEntry> CreateAsync(Guid userId, CreateRecurringEntryRequest request);
    Task<IEnumerable<RecurringEntry>> GetAllAsync(Guid userId);
    Task<RecurringEntry?> GetByIdAsync(Guid userId, Guid id);
    Task<IEnumerable<RecurringEntry>> GetByWalletAsync(Guid userId, Guid walletId);
    Task<RecurringEntry> UpdateAsync(Guid userId, UpdateRecurringEntryRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task ProcessPendingEntries();
}
