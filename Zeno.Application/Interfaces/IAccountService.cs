using Zeno.Domain.Wallet;

namespace Zeno.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<Account>> GetAccountsByUserAsync(Guid userId);
    Task<IEnumerable<Account>> GetAccountsByWalletAsync(Guid userId, Guid walletId);
    Task<Account?> GetAccountByIdAsync(Guid userId, Guid accountId);
    Task<Account> CreateAccountAsync(Guid userId, Account account);
    Task<Account> UpdateAccountAsync(Guid userId, Account account);
    Task DeleteAccountAsync(Guid userId, Guid accountId);
}