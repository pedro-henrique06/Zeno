using Zeno.Domain.Wallet;

namespace Zeno.Application.Interfaces;

public interface IWalletService
{
    Task<Wallet> CreateWallet(Wallet wallet);
    Task<Wallet> UpdateWallet(Wallet wallet);
    Task<Wallet> DeleteWallet(Guid id);
    Task<IEnumerable<Wallet>> GetAllWallets();
    Task<Wallet?> GetWalletById(Guid id);
}
