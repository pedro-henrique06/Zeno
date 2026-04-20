using EntryEntity = Zeno.Domain.Entry.Entry;
using HomeEntity = Zeno.Domain.Home.Home;
using HomeExpenseEntity = Zeno.Domain.Home.HomeExpense;
using HomeWalletEntity = Zeno.Domain.Home.HomeWallet;
using SplitResult = Zeno.Domain.Home.ExpenseSplitResult;

namespace Zeno.Domain.Interfaces;

public interface IHomeRepository
{
    Task<HomeEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<HomeEntity>> GetAllAsync();
    Task<HomeEntity> CreateAsync(HomeEntity home);
    Task<HomeEntity> UpdateAsync(HomeEntity home);
    Task DeleteAsync(Guid id);
    Task AddWalletAsync(Guid homeId, Guid walletId);
    Task RemoveWalletAsync(Guid homeId, Guid walletId);
    Task<IEnumerable<HomeWalletEntity>> GetWalletsByHomeAsync(Guid homeId);
    Task<HomeExpenseEntity> CreateExpenseAsync(HomeExpenseEntity expense);
    Task DeleteExpenseAsync(Guid expenseId);
    Task<IEnumerable<HomeExpenseEntity>> GetExpensesByMonthAsync(Guid homeId, int month, int year);
    Task<IEnumerable<SplitResult>> CalculateSplitAsync(Guid homeId, int month, int year);
    Task<IEnumerable<EntryEntity>> GetCreditsByWalletsAndMonthAsync(IEnumerable<Guid> walletIds, int month, int year);
}
