using WalletEntity = Zeno.Domain.Wallet.Wallet;

namespace Zeno.Domain.Interfaces;

public interface IWalletRepository
{
    Task<WalletEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<WalletEntity>> GetAllAsync();
    Task<WalletEntity> CreateAsync(WalletEntity wallet);
    Task<WalletEntity> UpdateAsync(WalletEntity wallet);
    Task DeleteAsync(Guid id);
    Task AddBalanceAsync(Guid id, decimal amount);
}
