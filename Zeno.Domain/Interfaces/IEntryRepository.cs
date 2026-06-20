using EntryEntity = Zeno.Domain.Entry.Entry;
using Zeno.Domain.Enum;

namespace Zeno.Domain.Interfaces;

public interface IEntryRepository
{
    Task<EntryEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<EntryEntity>> GetByMonthAsync(int month, int year, Guid walletId);
    Task<(IEnumerable<EntryEntity> Items, int TotalCount)> GetByMonthPagedAsync(int month, int year, Guid walletId, EntryType? type, Category? category, int page, int pageSize);
    Task<(IEnumerable<EntryEntity> Items, int TotalCount)> GetByMonthForUserPagedAsync(int month, int year, Guid userId, int page, int pageSize);
    Task<IEnumerable<EntryEntity>> GetFromDateAsync(Guid walletId, DateTime fromDate);
    Task<IEnumerable<EntryEntity>> GetFromDateForUserAsync(Guid userId, DateTime fromDate);
    Task<IEnumerable<EntryEntity>> GetUpToDateAsync(Guid walletId, DateTime toDate);
    Task<IEnumerable<EntryEntity>> GetUpToDateForUserAsync(Guid userId, DateTime toDate);
    Task<decimal> GetSumByKindAsync(Guid walletId, EntryKind kind, int month, int year);
    Task<decimal> GetTotalByTypeAndWalletAsync(int month, int year, Guid walletId, int type);
    Task<IEnumerable<(int Category, decimal Total)>> GetCategoryTotalsAsync(int month, int year, Guid walletId);
    Task<EntryEntity> CreateAsync(EntryEntity entry, object? transaction = null);
    Task<EntryEntity> UpdateAsync(EntryEntity entry, object? transaction = null);
    Task DeleteAsync(Guid id, object? transaction = null);
}
