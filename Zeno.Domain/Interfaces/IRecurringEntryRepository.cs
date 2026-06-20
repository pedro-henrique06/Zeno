using RecurringEntryEntity = Zeno.Domain.Recurring.RecurringEntry;

namespace Zeno.Domain.Interfaces;

public interface IRecurringEntryRepository
{
    Task<RecurringEntryEntity?> GetByIdAsync(Guid id);
    Task<RecurringEntryEntity?> GetByIdAndUserAsync(Guid id, Guid userId);
    Task<IEnumerable<RecurringEntryEntity>> GetByUserAsync(Guid userId);
    Task<IEnumerable<RecurringEntryEntity>> GetByWalletAsync(Guid walletId);
    Task<IEnumerable<RecurringEntryEntity>> GetActiveByDayAsync(int dayOfMonth);
    Task<RecurringEntryEntity> CreateAsync(RecurringEntryEntity entry);
    Task<RecurringEntryEntity> UpdateAsync(RecurringEntryEntity entry);
    Task DeleteAsync(Guid id);
}
