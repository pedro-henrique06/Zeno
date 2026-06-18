using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class HomeBudgetService : Zeno.Application.Interfaces.IHomeBudgetService
{
    private readonly IHomeRepository _repository;

    public HomeBudgetService(IHomeRepository repository)
    {
        _repository = repository;
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

        string alertMessage;
        if (isOverBudget)
        {
            alertMessage = string.Format("ATENÇÃO: As despesas da casa ({0:F1}% da renda) ultrapassaram o limite de 50% estabelecido pela regra 50/30/20. Limite: R$ {1:F2}, Gasto: R$ {2:F2}.", needsUsagePercentage, maxNeedsLimit, totalExpenses);
        }
        else if (totalExpenses > 0)
        {
            alertMessage = string.Format("Dentro do orçamento. Despesas em {0:F1}% da renda. Limite de necessidades: R$ {1:F2}.", needsUsagePercentage, maxNeedsLimit);
        }
        else
        {
            alertMessage = "Nenhuma despesa registrada neste mês.";
        }

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
}