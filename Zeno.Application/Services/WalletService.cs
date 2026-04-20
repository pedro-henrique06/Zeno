using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Validators;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;

namespace Zeno.Application.Services;

public class WalletService : IWalletService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWalletRepository _repository;

    public WalletService(IServiceProvider serviceProvider, IWalletRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
    }

    public async Task<Wallet> CreateWallet(Wallet wallet)
    {
        await ValidateAsync<WalletValidator, Wallet>(wallet);

        wallet.Id = Guid.NewGuid();

        return await _repository.CreateAsync(wallet);
    }

    public async Task<Wallet> UpdateWallet(Wallet wallet)
    {
        await ValidateAsync<UpdateWalletValidator, Wallet>(wallet);

        return await _repository.UpdateAsync(wallet);
    }

    public async Task<Wallet> DeleteWallet(Guid id)
    {
        var wallet = await _repository.GetByIdAsync(id)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Carteira não encontrada.")
                }));

        await _repository.DeleteAsync(id);

        return wallet;
    }

    public async Task<IEnumerable<Wallet>> GetAllWallets()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Wallet?> GetWalletById(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Wallet> AddSalary(Guid walletId, decimal amount)
    {
        await ValidateAsync<AddSalaryRequestValidator, AddSalaryRequest>(new AddSalaryRequest { Amount = amount });

        var wallet = await _repository.GetByIdAsync(walletId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(walletId), "Carteira não encontrada.")
                }));

        await _repository.AddBalanceAsync(walletId, amount);

        wallet.Balance += amount;
        return wallet;
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
