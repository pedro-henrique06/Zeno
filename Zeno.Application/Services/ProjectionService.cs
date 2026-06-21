using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Application.Validators;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class ProjectionService : IProjectionService
{
    private const int HistoryMonths = 3;

    private readonly IServiceProvider _serviceProvider;
    private readonly IWalletRepository _walletRepository;
    private readonly IEntryRepository _entryRepository;

    public ProjectionService(IServiceProvider serviceProvider, IWalletRepository walletRepository, IEntryRepository entryRepository)
    {
        _serviceProvider = serviceProvider;
        _walletRepository = walletRepository;
        _entryRepository = entryRepository;
    }

    public async Task<ProjectionResponse> SimulateAsync(Guid userId, ProjectionSimulationRequest request)
    {
        await ValidateAsync<ProjectionSimulationRequestValidator, ProjectionSimulationRequest>(request);

        var wallet = await _walletRepository.GetByIdAndUserAsync(request.WalletId!.Value, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        var (avgIncome, avgExpenses) = await CalculateBaselineAsync(wallet.Id);

        var months = ProjectMonths(wallet.Balance, avgIncome, avgExpenses, request);

        return new ProjectionResponse
        {
            WalletId = wallet.Id,
            CurrentBalance = wallet.Balance,
            AverageMonthlyIncome = Math.Round(avgIncome, 2),
            AverageMonthlyExpenses = Math.Round(avgExpenses, 2),
            ExtraExpenseAmount = request.ExtraExpenseAmount,
            IsRecurring = request.IsRecurring,
            Months = months,
            Summary = BuildSummary(months, request)
        };
    }

    private static List<ProjectionMonthResult> ProjectMonths(decimal startingBalance, decimal avgIncome, decimal avgExpenses, ProjectionSimulationRequest request)
    {
        var months = new List<ProjectionMonthResult>();
        var runningBalance = startingBalance;
        var referenceDate = DateTime.UtcNow;
        var maxNeedsLimit = avgIncome * 0.50m;

        for (var i = 1; i <= request.MonthsToProject; i++)
        {
            var targetDate = referenceDate.AddMonths(i);
            var extra = request.IsRecurring || i == 1 ? request.ExtraExpenseAmount : 0;

            var projectedExpenses = avgExpenses + extra;
            runningBalance += avgIncome - projectedExpenses;

            months.Add(new ProjectionMonthResult
            {
                Month = targetDate.Month,
                Year = targetDate.Year,
                ProjectedIncome = Math.Round(avgIncome, 2),
                ProjectedExpenses = Math.Round(projectedExpenses, 2),
                ProjectedBalance = Math.Round(runningBalance, 2),
                IsOverBudget = projectedExpenses > maxNeedsLimit
            });
        }

        return months;
    }

    private async Task<(decimal avgIncome, decimal avgExpenses)> CalculateBaselineAsync(Guid walletId)
    {
        var now = DateTime.UtcNow;
        decimal totalIncome = 0;
        decimal totalExpenses = 0;
        var monthsWithData = 0;

        for (var i = 0; i < HistoryMonths; i++)
        {
            var refDate = now.AddMonths(-i);
            var entries = (await _entryRepository.GetByMonthAsync(refDate.Month, refDate.Year, walletId)).ToList();

            if (entries.Count == 0)
                continue;

            monthsWithData++;
            totalIncome += entries.Where(e => e.Type == EntryType.Credit).Sum(e => e.Value);
            totalExpenses += entries.Where(e => e.Type == EntryType.Debit).Sum(e => e.Value);
        }

        if (monthsWithData == 0)
            return (0, 0);

        return (totalIncome / monthsWithData, totalExpenses / monthsWithData);
    }

    private static string BuildSummary(List<ProjectionMonthResult> months, ProjectionSimulationRequest request)
    {
        var firstNegative = months.FirstOrDefault(m => m.ProjectedBalance < 0);
        if (firstNegative is not null)
            return $"Atenção: com um gasto extra de R$ {request.ExtraExpenseAmount:F2}" +
                   $"{(request.IsRecurring ? " por mês" : "")}, seu saldo ficará negativo em {firstNegative.Month}/{firstNegative.Year}.";

        var overBudgetCount = months.Count(m => m.IsOverBudget);
        if (overBudgetCount > 0)
            return $"Esse gasto extra faria suas despesas ultrapassarem 50% da renda (regra 50/30/20) em {overBudgetCount} de {months.Count} meses projetados.";

        return "Seu orçamento permanece saudável mesmo considerando esse gasto extra.";
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
