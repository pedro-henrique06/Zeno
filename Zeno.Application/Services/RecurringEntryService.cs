using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Recurring;

namespace Zeno.Application.Services;

public class RecurringEntryService : IRecurringEntryService
{
    private readonly IValidator<CreateRecurringEntryRequest> _createValidator;
    private readonly IValidator<UpdateRecurringEntryRequest> _updateValidator;
    private readonly IRecurringEntryRepository _repository;
    private readonly IWalletRepository _walletRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecurringEntryService(
        IValidator<CreateRecurringEntryRequest> createValidator,
        IValidator<UpdateRecurringEntryRequest> updateValidator,
        IRecurringEntryRepository repository,
        IWalletRepository walletRepository,
        IEntryRepository entryRepository,
        IUnitOfWork unitOfWork)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _repository = repository;
        _walletRepository = walletRepository;
        _entryRepository = entryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringEntry> CreateAsync(Guid userId, CreateRecurringEntryRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var wallet = await _walletRepository.GetByIdAndUserAsync(request.WalletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        var entry = new RecurringEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WalletId = request.WalletId,
            Title = request.Title,
            Value = request.Value,
            Type = request.Type,
            Kind = request.Kind,
            Category = request.Category,
            DayOfMonth = request.DayOfMonth,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(entry);
    }

    public async Task<IEnumerable<RecurringEntry>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task<RecurringEntry?> GetByIdAsync(Guid userId, Guid id)
    {
        return await _repository.GetByIdAndUserAsync(id, userId);
    }

    public async Task<IEnumerable<RecurringEntry>> GetByWalletAsync(Guid userId, Guid walletId)
    {
        var wallet = await _walletRepository.GetByIdAndUserAsync(walletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        return await _repository.GetByWalletAsync(walletId);
    }

    public async Task<RecurringEntry> UpdateAsync(Guid userId, UpdateRecurringEntryRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var existing = await _repository.GetByIdAndUserAsync(request.Id, userId);
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Lançamento recorrente não encontrado.")
                }));

        existing.Title = request.Title;
        existing.Value = request.Value;
        existing.Type = request.Type;
        existing.Kind = request.Kind;
        existing.Category = request.Category;
        existing.DayOfMonth = request.DayOfMonth;
        existing.IsActive = request.IsActive;

        return await _repository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var existing = await _repository.GetByIdAndUserAsync(id, userId);
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Lançamento recorrente não encontrado.")
                }));

        await _repository.DeleteAsync(id);
    }

    public async Task ProcessPendingEntries()
    {
        var today = DateTime.UtcNow;
        var dayOfMonth = today.Day;
        var entries = await _repository.GetActiveByDayAsync(dayOfMonth);

        foreach (var recurringEntry in entries)
        {
            var alreadyProcessed = recurringEntry.LastProcessedAt.HasValue &&
                recurringEntry.LastProcessedAt.Value.Year == today.Year &&
                recurringEntry.LastProcessedAt.Value.Month == today.Month;

            if (alreadyProcessed)
                continue;

            await _unitOfWork.BeginAsync();
            try
            {
                var signedAmount = recurringEntry.Type == EntryType.Credit ? recurringEntry.Value : -recurringEntry.Value;

                var newEntry = new Entry
                {
                    Id = Guid.NewGuid(),
                    Title = recurringEntry.Title,
                    Value = recurringEntry.Value,
                    Type = recurringEntry.Type,
                    Kind = recurringEntry.Kind,
                    Category = recurringEntry.Category,
                    Date = today,
                    WalletId = recurringEntry.WalletId,
                    Description = $"{recurringEntry.Title} - lançamento recorrente do dia {recurringEntry.DayOfMonth}"
                };

                await _entryRepository.CreateAsync(newEntry, _unitOfWork.Transaction);
                await _walletRepository.AddBalanceAsync(recurringEntry.WalletId, signedAmount, _unitOfWork.Transaction);

                recurringEntry.LastProcessedAt = today;
                await _repository.UpdateAsync(recurringEntry);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
