using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;

namespace Zeno.Application.Services;

public class AccountService : IAccountService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAccountRepository _accountRepository;
    private readonly IWalletRepository _walletRepository;

    public AccountService(
        IServiceProvider serviceProvider,
        IAccountRepository accountRepository,
        IWalletRepository walletRepository)
    {
        _serviceProvider = serviceProvider;
        _accountRepository = accountRepository;
        _walletRepository = walletRepository;
    }

    public async Task<IEnumerable<Account>> GetAccountsByUserAsync(Guid userId)
    {
        return await _accountRepository.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Account>> GetAccountsByWalletAsync(Guid userId, Guid walletId)
    {
        var wallet = await _walletRepository.GetByIdAndUserAsync(walletId, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(walletId), "Carteira não encontrada.")
                }));

        return await _accountRepository.GetByWalletIdAsync(walletId);
    }

    public async Task<Account?> GetAccountByIdAsync(Guid userId, Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null) return null;

        var wallet = await _walletRepository.GetByIdAndUserAsync(account.WalletId, userId);
        return wallet != null ? account : null;
    }

    public async Task<Account> CreateAccountAsync(Guid userId, Account account)
    {
        var wallet = await _walletRepository.GetByIdAndUserAsync(account.WalletId, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(account.WalletId), "Carteira não encontrada.")
                }));

        account.Id = Guid.NewGuid();
        account.CreatedAt = DateTime.UtcNow;

        return await _accountRepository.CreateAsync(account);
    }

    public async Task<Account> UpdateAccountAsync(Guid userId, Account account)
    {
        var existing = await _accountRepository.GetByIdAsync(account.Id!.Value)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(account.Id), "Conta não encontrada.")
                }));

        var wallet = await _walletRepository.GetByIdAndUserAsync(existing.WalletId, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(account.Id), "Conta não encontrada.")
                }));

        existing.Name = account.Name;
        existing.Bank = account.Bank;
        existing.Type = account.Type;

        return await _accountRepository.UpdateAsync(existing);
    }

    public async Task DeleteAccountAsync(Guid userId, Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(accountId), "Conta não encontrada.")
                }));

        var wallet = await _walletRepository.GetByIdAndUserAsync(account.WalletId, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(accountId), "Conta não encontrada.")
                }));

        await _accountRepository.DeleteAsync(accountId);
    }
}