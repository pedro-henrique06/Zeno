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

    public async Task<Salary> CreateSalary(Salary salary)
    {
        await ValidateAsync<SalaryValidator, Salary>(salary);

        salary.Id = Guid.NewGuid();

        return await _salaryRepository.CreateAsync(salary);
    }

    public async Task<Salary> UpdateSalary(Salary salary)
    {
        await ValidateAsync<SalaryValidator, Salary>(salary);

        return await _salaryRepository.UpdateAsync(salary);
    }

    public async Task<Salary> DeleteSalary(Guid id)
    {
        var salary = await _salaryRepository.GetByIdAsync(id)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Salário não encontrado.")
                }));

        await _salaryRepository.DeleteAsync(id);

        return salary;
    }

    public async Task<Salary?> GetSalaryById(Guid id)
    {
        return await _salaryRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Salary>> GetSalariesByWallet(Guid walletId)
    {
        return await _salaryRepository.GetByWalletAsync(walletId);
    }

    public async Task<IEnumerable<Salary>> GetSalariesByUser(Guid userId)
    {
        return await _salaryRepository.GetByUserIdAsync(userId);
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
