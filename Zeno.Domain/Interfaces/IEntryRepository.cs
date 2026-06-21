using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Domain.Interfaces;

public interface IEntryRepository
{
    Task<EntryEntity?> GetByIdAsync(Guid id);
    Task<(IEnumerable<EntryEntity> Items, int TotalCount)> GetByMonthForUserPagedAsync(int month, int year, Guid userId, int page, int pageSize);
    Task<EntryEntity> CreateAsync(EntryEntity entry);
    Task<EntryEntity> UpdateAsync(EntryEntity entry);
    Task DeleteAsync(Guid id);
}
