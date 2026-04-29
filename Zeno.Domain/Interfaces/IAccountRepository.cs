using Zeno.Domain.Wallet;

namespace Zeno.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByWalletIdAsync(Guid walletId);
    Task<Account> CreateAsync(Account account);
    Task<Account> UpdateAsync(Account account);
    Task DeleteAsync(Guid id);
    Task UpdateBalanceAsync(Guid id, decimal newBalance);
    Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId);
}