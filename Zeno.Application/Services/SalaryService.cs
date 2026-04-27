using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Validators;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Salary;

namespace Zeno.Application.Services;

public class SalaryService : ISalaryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISalaryRepository _salaryRepository;
    private readonly IWalletRepository _walletRepository;

    public SalaryService(IServiceProvider serviceProvider, ISalaryRepository salaryRepository, IWalletRepository walletRepository)
    {
        _serviceProvider = serviceProvider;
        _salaryRepository = salaryRepository;
        _walletRepository = walletRepository;
    }

    public async Task<Salary> CreateSalary(Guid userId, Salary salary)
    {
        await ValidateAsync<SalaryValidator, Salary>(salary);

        var wallet = await _walletRepository.GetByIdAndUserAsync(salary.WalletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        salary.Id = Guid.NewGuid();

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

    public async Task<IEnumerable<Salary>> GetSalariesByWallet(Guid userId, Guid walletId)
    {
        var wallet = await _walletRepository.GetByIdAndUserAsync(walletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        return await _salaryRepository.GetByWalletAsync(walletId);
    }

    public async Task ProcessPendingSalaries()
    {
        var today = DateTime.UtcNow.Day;
        var pendingSalaries = await _salaryRepository.GetPendingSalariesAsync(today);

        foreach (var salary in pendingSalaries)
        {
            await _walletRepository.AddBalanceAsync(salary.WalletId, salary.Amount);
            await _salaryRepository.MarkProcessedAsync(salary.Id!.Value);
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
