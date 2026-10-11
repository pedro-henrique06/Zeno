using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Domain.Interfaces;

public interface IEntryRepository
{
    Task<EntryEntity?> GetByIdAsync(Guid id);
    Task<(IEnumerable<EntryEntity> Items, int TotalCount)> GetByMonthForUserPagedAsync(int month, int year, Guid userId, int page, int pageSize);
    Task<IEnumerable<EntryEntity>> GetByUserInRangeAsync(Guid userId, DateTime? start, DateTime? end);
    Task<IEnumerable<EntryEntity>> GetRecurringBeforeAsync(Guid userId, DateTime before);
    Task<decimal> GetSignedBalanceBeforeAsync(Guid userId, DateTime before);
    Task<EntryEntity> CreateAsync(EntryEntity entry);
    Task<EntryEntity> UpdateAsync(EntryEntity entry);
    Task DeleteAsync(Guid id);
    Task ClearTagReferencesAsync(Guid tagId);
    Task DeleteByUserAsync(Guid userId);

    /// <summary>Sets the kind of every entry captured from a card; returns how many changed.</summary>
    Task<long> SetKindByCaptureCardAsync(Guid userId, Guid captureCardId, Zeno.Domain.Enum.EntryKind kind);
    Task<IEnumerable<EntryEntity>> GetRecurringByHouseAsync(Guid userId, Guid houseId);
    Task MultiplyValuesForUserAsync(Guid userId, decimal factor);
}
