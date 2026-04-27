using Zeno.Domain.Wallet;

namespace Zeno.Application.Interfaces;

public interface IWalletService
{
    Task<Wallet> CreateWallet(Guid userId, Wallet wallet);
    Task<Wallet> UpdateWallet(Guid userId, Wallet wallet);
    Task<Wallet> DeleteWallet(Guid userId, Guid id);
    Task<IEnumerable<Wallet>> GetAllWallets(Guid userId);
    Task<Wallet?> GetWalletById(Guid userId, Guid id);
    Task<Wallet> AddSalary(Guid userId, Guid walletId, decimal amount);
    Task<IEnumerable<Wallet>> GetWalletsByUser(Guid userId);
}
