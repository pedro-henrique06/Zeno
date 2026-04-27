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

    public async Task<Wallet> CreateWallet(Guid userId, Wallet wallet)
    {
        wallet.Id = Guid.NewGuid();
        wallet.UserId = userId;

        await ValidateAsync<WalletValidator, Wallet>(wallet);

        return await _repository.CreateAsync(wallet);
    }

    public async Task<Wallet> UpdateWallet(Guid userId, Wallet wallet)
    {
        await ValidateAsync<UpdateWalletValidator, Wallet>(wallet);

        var existing = await _repository.GetByIdAndUserAsync(wallet.Id!.Value, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(wallet.Id), "Carteira não encontrada.")
                }));

        return await _repository.UpdateAsync(wallet);
    }

    public async Task<Wallet> DeleteWallet(Guid userId, Guid id)
    {
        var wallet = await _repository.GetByIdAndUserAsync(id, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Carteira não encontrada.")
                }));

        await _repository.DeleteAsync(id);

        return wallet;
    }

    public async Task<IEnumerable<Wallet>> GetAllWallets(Guid userId)
    {
        return await _repository.GetAllByUserAsync(userId);
    }

    public async Task<IEnumerable<Wallet>> GetWalletsByUser(Guid userId)
    {
        return await GetAllWallets(userId);
    }

    public async Task<Wallet?> GetWalletById(Guid userId, Guid id)
    {
        return await _repository.GetByIdAndUserAsync(id, userId);
    }

    public async Task<Wallet> AddSalary(Guid userId, Guid walletId, decimal amount)
    {
        await ValidateAsync<AddSalaryRequestValidator, AddSalaryRequest>(new AddSalaryRequest { Amount = amount });

        var wallet = await _repository.GetByIdAndUserAsync(walletId, userId)
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
