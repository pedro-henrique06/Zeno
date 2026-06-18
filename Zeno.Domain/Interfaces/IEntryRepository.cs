using EntryEntity = Zeno.Domain.Entry.Entry;
using Zeno.Domain.Enum;

namespace Zeno.Domain.Interfaces;

public interface IEntryRepository
{
    Task<EntryEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<EntryEntity>> GetByMonthAsync(int month, int year, Guid walletId);
    Task<(IEnumerable<EntryEntity> Items, int TotalCount)> GetByMonthPagedAsync(int month, int year, Guid walletId, EntryType? type, Category? category, int page, int pageSize);
    Task<EntryEntity> CreateAsync(EntryEntity entry, object? transaction = null);
    Task<EntryEntity> UpdateAsync(EntryEntity entry, object? transaction = null);
    Task DeleteAsync(Guid id, object? transaction = null);
    Task<decimal> GetTotalByTypeAndWalletAsync(int month, int year, Guid walletId, int type);
    Task<IEnumerable<(int Category, decimal Total)>> GetCategoryTotalsAsync(int month, int year, Guid walletId);
}