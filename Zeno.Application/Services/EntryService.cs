using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Validators;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class EntryService : IEntryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntryRepository _repository;
    private readonly IWalletRepository _walletRepository;

    public EntryService(IServiceProvider serviceProvider, IEntryRepository repository, IWalletRepository walletRepository)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
        _walletRepository = walletRepository;
    }

    public async Task<Entry> CreateEntry(Guid userId, Entry entry)
    {
        await ValidateAsync<EntryValidator, Entry>(entry);

        var wallet = await _walletRepository.GetByIdAndUserAsync(entry.WalletId!.Value, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        entry.Id = Guid.NewGuid();

        var created = await _repository.CreateAsync(entry);

        await UpdateWalletBalance(entry.WalletId!.Value, entry.Type, entry.Value);

        return created;
    }

    public async Task<Entry> UpdateEntry(Guid userId, Entry entry)
    {
        await ValidateAsync<UpdateEntryValidator, Entry>(entry);

        var oldEntry = await _repository.GetByIdAsync(entry.Id!.Value);
        if (oldEntry is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(entry.Id), "Lançamento não encontrado.")
                }));

        var oldWallet = await _walletRepository.GetByIdAndUserAsync(oldEntry.WalletId!.Value, userId);
        if (oldWallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        await ReverseWalletBalance(oldEntry.WalletId!.Value, oldEntry.Type, oldEntry.Value);

        await _repository.UpdateAsync(entry);

        await UpdateWalletBalance(entry.WalletId!.Value, entry.Type, entry.Value);

        return entry;
    }

    public async Task<Entry> DeleteEntry(Guid userId, Guid id)
    {
        var entry = new Entry { Id = id };

        await ValidateAsync<DeleteEntryValidator, Entry>(entry);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Lançamento não encontrado.")
                }));

        var wallet = await _walletRepository.GetByIdAndUserAsync(existing.WalletId!.Value, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        await ReverseWalletBalance(existing.WalletId!.Value, existing.Type, existing.Value);

        await _repository.DeleteAsync(id);

        return existing;
    }

    public async Task<IEnumerable<Entry>> GetEntriesByMonth(Guid userId, GetEntriesByMonthQuery query)
    {
        await ValidateAsync<GetEntriesByMonthQueryValidator, GetEntriesByMonthQuery>(query);

        var wallet = await _walletRepository.GetByIdAndUserAsync(query.WalletId!.Value, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        return await _repository.GetByMonthAsync(query.Month!.Value, query.Year!.Value, query.WalletId!.Value);
    }

    private async Task UpdateWalletBalance(Guid walletId, EntryType type, decimal value)
    {
        var amount = type == EntryType.Credit ? value : -value;
        await _walletRepository.AddBalanceAsync(walletId, amount);
    }

    private async Task ReverseWalletBalance(Guid walletId, EntryType type, decimal value)
    {
        var amount = type == EntryType.Credit ? -value : value;
        await _walletRepository.AddBalanceAsync(walletId, amount);
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
