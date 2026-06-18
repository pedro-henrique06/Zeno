using Zeno.Application.Exceptions;
using Zeno.Application.Requests;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.RecurringExpense;

namespace Zeno.Application.Services;

public class RecurringExpenseService : Zeno.Application.Interfaces.IRecurringExpenseService
{
    private readonly IRecurringExpenseRepository _repository;
    private readonly IWalletRepository _walletRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecurringExpenseService(
        IRecurringExpenseRepository repository,
        IWalletRepository walletRepository,
        IEntryRepository entryRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _walletRepository = walletRepository;
        _entryRepository = entryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringExpense> CreateAsync(Guid userId, CreateRecurringExpenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Title", "Título é obrigatório.") }));

        if (request.Value <= 0)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Value", "Valor deve ser maior que zero.") }));

        if (request.DayOfMonth < 1 || request.DayOfMonth > 31)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("DayOfMonth", "Dia do mês deve ser entre 1 e 31.") }));

        var wallet = await _walletRepository.GetByIdAndUserAsync(request.WalletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("WalletId", "Carteira não encontrada.") }));

        var expense = new RecurringExpense
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WalletId = request.WalletId,
            Title = request.Title,
            Value = request.Value,
            DayOfMonth = request.DayOfMonth,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(expense);
    }

    public async Task<IEnumerable<RecurringExpense>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task<RecurringExpense?> GetByIdAsync(Guid userId, Guid id)
    {
        var expense = await _repository.GetByIdAsync(id);
        if (expense is null || expense.UserId != userId)
            return null;
        return expense;
    }

    public async Task<RecurringExpense> UpdateAsync(Guid userId, UpdateRecurringExpenseRequest request)
    {
        var expense = await _repository.GetByIdAsync(request.Id);
        if (expense is null || expense.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Despesa recorrente não encontrada.") }));

        expense.Title = request.Title;
        expense.Value = request.Value;
        expense.DayOfMonth = request.DayOfMonth;
        expense.IsActive = request.IsActive;

        return await _repository.UpdateAsync(expense);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var expense = await _repository.GetByIdAsync(id);
        if (expense is null || expense.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Despesa recorrente não encontrada.") }));

        await _repository.DeleteAsync(id);
    }

    public async Task ProcessMonthlyAsync()
    {
        var today = DateTime.UtcNow;
        var dayOfMonth = today.Day;
        var expenses = await _repository.GetActiveByDayAsync(dayOfMonth);

        foreach (var expense in expenses)
        {
            var alreadyProcessed = expense.LastProcessedAt.HasValue &&
                expense.LastProcessedAt.Value.Year == today.Year &&
                expense.LastProcessedAt.Value.Month == today.Month;

            if (alreadyProcessed)
                continue;

            await _unitOfWork.BeginAsync();
            try
            {
                var entry = new Entry
                {
                    Id = Guid.NewGuid(),
                    Title = expense.Title,
                    Value = expense.Value,
                    Type = EntryType.Debit,
                    Category = Category.Utilities,
                    Date = today,
                    WalletId = expense.WalletId
                };

                await _entryRepository.CreateAsync(entry, _unitOfWork.Transaction);
                await _walletRepository.AddBalanceAsync(expense.WalletId, -expense.Value, _unitOfWork.Transaction);

                expense.LastProcessedAt = today;
                await _repository.UpdateAsync(expense);

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