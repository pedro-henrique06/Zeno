using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Validators;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Salary;

namespace Zeno.Application.Services;

public class SalaryService : ISalaryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISalaryRepository _salaryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IEntryRepository _entryRepository;

    public SalaryService(IServiceProvider serviceProvider, ISalaryRepository salaryRepository, IAccountRepository accountRepository, IEntryRepository entryRepository)
    {
        _serviceProvider = serviceProvider;
        _salaryRepository = salaryRepository;
        _accountRepository = accountRepository;
        _entryRepository = entryRepository;
    }

    public async Task<Salary> CreateSalary(Guid userId, Salary salary)
    {
        salary.Id = Guid.NewGuid();
        salary.UserId = userId;

        await ValidateAsync<SalaryValidator, Salary>(salary);

        var account = await _accountRepository.GetByIdAsync(salary.AccountId);
        if (account is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("AccountId", "Conta não encontrada.")
                }));

        return await _salaryRepository.CreateAsync(salary);
    }

    public async Task<Salary> UpdateSalary(Guid userId, Salary salary)
    {
        await ValidateAsync<SalaryValidator, Salary>(salary);

        var existing = await _salaryRepository.GetByIdAndUserAsync(salary.Id!.Value, userId);
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(salary.Id), "Salário não encontrado.")
                }));

        return await _salaryRepository.UpdateAsync(salary);
    }

    public async Task<Salary> DeleteSalary(Guid userId, Guid id)
    {
        var salary = await _salaryRepository.GetByIdAndUserAsync(id, userId);
        if (salary is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Salário não encontrado.")
                }));

        await _salaryRepository.DeleteAsync(id);

        return salary;
    }

    public async Task<Salary?> GetSalaryById(Guid userId, Guid id)
    {
        return await _salaryRepository.GetByIdAndUserAsync(id, userId);
    }

    public async Task<IEnumerable<Salary>> GetSalariesByUser(Guid userId)
    {
        return await _salaryRepository.GetByUserAsync(userId);
    }

    public async Task<IEnumerable<Salary>> GetSalariesByWallet(Guid userId, Guid walletId)
    {
        return await _salaryRepository.GetByAccountAsync(walletId);
    }

    public async Task ProcessPendingSalaries()
    {
        var today = DateTime.UtcNow.Day;
        var pendingSalaries = await _salaryRepository.GetPendingSalariesAsync(today);

        foreach (var salary in pendingSalaries)
        {
            var account = await _accountRepository.GetByIdAsync(salary.AccountId);
            if (account is not null)
            {
                var newBalance = account.Balance + salary.Amount;
                await _accountRepository.UpdateBalanceAsync(salary.AccountId, newBalance);

                var entry = new Entry
                {
                    Id = Guid.NewGuid(),
                    Title = salary.Description,
                    Value = salary.Amount,
                    Type = EntryType.Credit,
                    Category = Category.Salary,
                    Date = DateTime.UtcNow,
                    WalletId = account.WalletId,
                    Description = $"Salário creditado dia {salary.DayOfMonth}"
                };
                await _entryRepository.CreateAsync(entry);

                await _salaryRepository.MarkProcessedAsync(salary.Id!.Value);
            }
        }
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}