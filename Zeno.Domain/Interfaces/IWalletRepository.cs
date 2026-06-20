using WalletEntity = Zeno.Domain.Wallet.Wallet;

namespace Zeno.Domain.Interfaces;

public interface IWalletRepository
{
    Task<WalletEntity?> GetByIdAsync(Guid id);
    Task<WalletEntity?> GetByIdAndUserAsync(Guid id, Guid userId);
    Task<IEnumerable<WalletEntity>> GetAllByUserAsync(Guid userId);
    Task<WalletEntity> CreateAsync(WalletEntity wallet);
    Task<WalletEntity> UpdateAsync(WalletEntity wallet);
    Task DeleteAsync(Guid id);
    Task AddBalanceAsync(Guid id, decimal amount, object? transaction = null);
    Task<decimal> GetTotalByUserAndMonthAsync(Guid userId, int month, int year);
    Task UpdateBudgetAsync(Guid id, decimal? dailyBudget);
}
