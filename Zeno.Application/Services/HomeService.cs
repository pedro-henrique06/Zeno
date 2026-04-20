using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Validators;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class HomeService : IHomeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHomeRepository _repository;

    public HomeService(IServiceProvider serviceProvider, IHomeRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
    }

    public async Task<Home> CreateHome(Home home)
    {
        await ValidateAsync<HomeValidator, Home>(home);

        home.Id = Guid.NewGuid();

        return await _repository.CreateAsync(home);
    }

    public async Task<Home> UpdateHome(Home home)
    {
        await ValidateAsync<HomeValidator, Home>(home);

        return await _repository.UpdateAsync(home);
    }

    public async Task<Home> DeleteHome(Guid id)
    {
        var home = await _repository.GetByIdAsync(id)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Casa não encontrada.")
                }));

        await _repository.DeleteAsync(id);

        return home;
    }

    public async Task<Home?> GetHomeById(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Home>> GetAllHomes()
    {
        return await _repository.GetAllAsync();
    }

    public async Task AddWalletToHome(Guid homeId, Guid walletId)
    {
        await _repository.AddWalletAsync(homeId, walletId);
    }

    public async Task RemoveWalletFromHome(Guid homeId, Guid walletId)
    {
        await _repository.RemoveWalletAsync(homeId, walletId);
    }

    public async Task<HomeExpense> CreateExpense(HomeExpense expense)
    {
        await ValidateAsync<HomeExpenseValidator, HomeExpense>(expense);

        expense.Id = Guid.NewGuid();

        return await _repository.CreateExpenseAsync(expense);
    }

    public async Task DeleteExpense(Guid expenseId)
    {
        await _repository.DeleteExpenseAsync(expenseId);
    }

    public async Task<IEnumerable<HomeExpense>> GetExpensesByMonth(Guid homeId, int month, int year)
    {
        return await _repository.GetExpensesByMonthAsync(homeId, month, year);
    }

    public async Task<IEnumerable<ExpenseSplitResult>> CalculateExpenseSplit(Guid homeId, int month, int year)
    {
        return await _repository.CalculateSplitAsync(homeId, month, year);
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
