using Zeno.Domain.Wallet;

namespace Zeno.Application.Interfaces;

public interface IWalletService
{
    Task<Wallet> CreateWallet(Wallet wallet);
    Task<Wallet> UpdateWallet(Wallet wallet);
    Task<Wallet> DeleteWallet(Guid id);
    Task<IEnumerable<Wallet>> GetAllWallets();
    Task<IEnumerable<Wallet>> GetWalletsByUser(Guid userId);
    Task<Wallet?> GetWalletById(Guid id);
    Task<Wallet> AddSalary(Guid walletId, decimal amount);
}
