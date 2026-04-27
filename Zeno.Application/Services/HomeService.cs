using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses;
using Zeno.Application.Validators;
using Zeno.Domain.Enum;
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

    public async Task<Home> CreateHome(Guid userId, Home home)
    {
        await ValidateAsync<HomeValidator, Home>(home);

        home.Id = Guid.NewGuid();

        var created = await _repository.CreateAsync(home);

        await _repository.AddMemberAsync(home.Id!.Value, userId, (int)HomeRole.Admin);

        return created;
    }

    public async Task<Home> UpdateHome(Guid userId, Home home)
    {
        await ValidateAsync<HomeValidator, Home>(home);

        var isAdmin = await _repository.IsAdminAsync(home.Id!.Value, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(home.Id), "Apenas o administrador pode atualizar a casa.")
                }));

        return await _repository.UpdateAsync(home);
    }

    public async Task<Home> DeleteHome(Guid userId, Guid id)
    {
        var isAdmin = await _repository.IsAdminAsync(id, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Apenas o administrador pode excluir a casa.")
                }));

        var home = await _repository.GetByIdAsync(id);
        if (home is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Casa não encontrada.")
                }));

        await _repository.DeleteAsync(id);

        return home;
    }

    public async Task<Home?> GetHomeById(Guid userId, Guid id)
    {
        return await _repository.GetByIdAndMemberAsync(id, userId);
    }

    public async Task<IEnumerable<Home>> GetAllHomes(Guid userId)
    {
        return await _repository.GetAllByUserAsync(userId);
    }

    public async Task AddWalletToHome(Guid userId, Guid homeId, Guid walletId)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        await _repository.AddWalletAsync(homeId, walletId);
    }

    public async Task RemoveWalletFromHome(Guid userId, Guid homeId, Guid walletId)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Apenas o administrador pode remover carteiras.")
                }));

        await _repository.RemoveWalletAsync(homeId, walletId);
    }

    public async Task<HomeExpense> CreateExpense(Guid userId, HomeExpense expense)
    {
        await ValidateAsync<HomeExpenseValidator, HomeExpense>(expense);

        var isMember = await _repository.IsMemberAsync(expense.HomeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(expense.HomeId), "Você não é membro desta casa.")
                }));

        expense.Id = Guid.NewGuid();

        return await _repository.CreateExpenseAsync(expense);
    }

    public async Task DeleteExpense(Guid userId, Guid expenseId)
    {
        var isAdmin = await _repository.IsAdminAsync(expenseId, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(expenseId), "Apenas o administrador pode remover despesas.")
                }));

        await _repository.DeleteExpenseAsync(expenseId);
    }

    public async Task<IEnumerable<HomeExpense>> GetExpensesByMonth(Guid userId, Guid homeId, int month, int year)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        return await _repository.GetExpensesByMonthAsync(homeId, month, year);
    }

    public async Task<IEnumerable<ExpenseSplitResult>> CalculateExpenseSplit(Guid userId, Guid homeId, int month, int year)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        return await _repository.CalculateSplitAsync(homeId, month, year);
    }

    public async Task InviteMember(Guid adminUserId, Guid homeId, Guid invitedUserId)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, adminUserId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Apenas o administrador pode convidar membros.")
                }));

        var alreadyMember = await _repository.IsMemberAsync(homeId, invitedUserId);
        if (alreadyMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(invitedUserId), "Este usuário já é membro da casa.")
                }));

        await _repository.AddMemberAsync(homeId, invitedUserId, (int)HomeRole.Member);
    }

    public async Task RemoveMember(Guid adminUserId, Guid homeId, Guid memberUserId)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, adminUserId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Apenas o administrador pode remover membros.")
                }));

        if (adminUserId == memberUserId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(memberUserId), "O administrador não pode remover a si mesmo.")
                }));

        await _repository.RemoveMemberAsync(homeId, memberUserId);
    }

    public async Task<IEnumerable<HomeMember>> GetMembers(Guid userId, Guid homeId)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        return await _repository.GetMembersByHomeAsync(homeId);
    }

    public async Task<BudgetAlertResponse> GetBudgetAlertAsync(Guid userId, Guid homeId, int month, int year)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        var totalIncome = await _repository.GetTotalIncomeAsync(homeId, month, year);
        var totalExpenses = await _repository.GetTotalExpensesAsync(homeId, month, year);

        var maxNeedsLimit = totalIncome * 0.50m;
        var wantsLimit = totalIncome * 0.30m;
        var savingsLimit = totalIncome * 0.20m;
        var needsUsagePercentage = totalIncome > 0 ? (totalExpenses / totalIncome) * 100 : 0;
        var isOverBudget = totalExpenses > maxNeedsLimit;

        var alertMessage = isOverBudget
            ? $"ATENÇÃO: As despesas da casa ({needsUsagePercentage:F1}% da renda) ultrapassaram o limite de 50% estabelecido pela regra 50/30/20. Limite: R$ {maxNeedsLimit:F2}, Gasto: R$ {totalExpenses:F2}."
            : totalExpenses > 0
                ? $"Dentro do orçamento. Despesas em {needsUsagePercentage:F1}% da renda. Limite de necessidades: R$ {maxNeedsLimit:F2}."
                : "Nenhuma despesa registrada neste mês.";

        return new BudgetAlertResponse
        {
            HomeId = homeId,
            Month = month,
            Year = year,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            MaxNeedsLimit = maxNeedsLimit,
            NeedsUsagePercentage = Math.Round(needsUsagePercentage, 2),
            WantsLimit = wantsLimit,
            SavingsLimit = savingsLimit,
            IsOverBudget = isOverBudget,
            AlertMessage = alertMessage
        };
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
