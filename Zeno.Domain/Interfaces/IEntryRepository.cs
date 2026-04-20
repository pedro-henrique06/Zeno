using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Domain.Interfaces;

public interface IEntryRepository
{
    Task<EntryEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<EntryEntity>> GetByMonthAsync(int month, int year, Guid walletId);
    Task<EntryEntity> CreateAsync(EntryEntity entry);
    Task<EntryEntity> UpdateAsync(EntryEntity entry);
    Task DeleteAsync(Guid id);
}
