using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Application.Validators;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Wallet;

namespace Zeno.Application.Services;

public class BalanceService : IBalanceService
{
    private const int HistoryMonths = 3;
    private const decimal AvgDaysInMonth = 30m;

    private readonly IServiceProvider _serviceProvider;
    private readonly IWalletRepository _walletRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IRecurringEntryRepository _recurringEntryRepository;

    public BalanceService(
        IServiceProvider serviceProvider,
        IWalletRepository walletRepository,
        IEntryRepository entryRepository,
        IRecurringEntryRepository recurringEntryRepository)
    {
        _serviceProvider = serviceProvider;
        _walletRepository = walletRepository;
        _entryRepository = entryRepository;
        _recurringEntryRepository = recurringEntryRepository;
    }

    public async Task<DailyBalancesResponse> GetDailyBalancesAsync(Guid userId, Guid walletId, int month, int year)
    {
        await GetOwnedWalletAsync(userId, walletId);

        var endOfMonth = EndOfMonth(month, year);
        var entries = await _entryRepository.GetUpToDateAsync(walletId, endOfMonth);

        return new DailyBalancesResponse
        {
            WalletId = walletId,
            Month = month,
            Year = year,
            Days = BuildDailySeries(entries, month, year)
        };
    }

    public async Task<DailyBalancesResponse> GetAggregatedDailyBalancesAsync(Guid userId, int month, int year)
    {
        var endOfMonth = EndOfMonth(month, year);
        var entries = await _entryRepository.GetUpToDateForUserAsync(userId, endOfMonth);

        return new DailyBalancesResponse
        {
            WalletId = null,
            Month = month,
            Year = year,
            Days = BuildDailySeries(entries, month, year)
        };
    }

    public async Task<DailyAverageResponse> GetDailyAverageAsync(Guid userId, Guid walletId, int months)
    {
        await GetOwnedWalletAsync(userId, walletId);

        var (avgIncome, avgExpenses) = await CalculateMonthlyBaselineAsync(walletId, months);

        var avgDailyIncome = avgIncome / AvgDaysInMonth;
        var avgDailyExpense = avgExpenses / AvgDaysInMonth;

        return new DailyAverageResponse
        {
            WalletId = walletId,
            Months = months,
            AverageDailyIncome = Math.Round(avgDailyIncome, 2),
            AverageDailyExpense = Math.Round(avgDailyExpense, 2),
            AverageDailyNet = Math.Round(avgDailyIncome - avgDailyExpense, 2)
        };
    }

    public async Task<ForecastResponse> GetForecastAsync(Guid userId, Guid walletId, int months)
    {
        var wallet = await GetOwnedWalletAsync(userId, walletId);

        var (avgIncome, avgExpenses) = await CalculateMonthlyBaselineAsync(walletId, HistoryMonths);
        var baselineMonthlyNet = avgIncome - avgExpenses;

        var recurringEntries = (await _recurringEntryRepository.GetByWalletAsync(walletId))
            .Where(r => r.IsActive)
            .ToList();
        var recurringMonthlyNet = recurringEntries.Sum(r => r.Type == EntryType.Credit ? r.Value : -r.Value);

        var variableDailyNet = (baselineMonthlyNet - recurringMonthlyNet) / AvgDaysInMonth;

        var running = wallet.Balance;
        var start = DateTime.UtcNow.Date;
        var end = start.AddMonths(months);
        var days = new List<DailyBalanceEntry>();

        for (var date = start.AddDays(1); date <= end; date = date.AddDays(1))
        {
            running += variableDailyNet;

            foreach (var recurring in recurringEntries.Where(r => r.DayOfMonth == date.Day))
                running += recurring.Type == EntryType.Credit ? recurring.Value : -recurring.Value;

            days.Add(new DailyBalanceEntry { Date = date, Balance = Math.Round(running, 2) });
        }

        return new ForecastResponse
        {
            WalletId = walletId,
            Months = months,
            CurrentBalance = wallet.Balance,
            Days = days
        };
    }

    public async Task<CardInvoiceResponse> GetCardInvoiceAsync(Guid userId, Guid walletId, int month, int year)
    {
        await GetOwnedWalletAsync(userId, walletId);

        var total = await _entryRepository.GetSumByKindAsync(walletId, EntryKind.Cartao, month, year);

        return new CardInvoiceResponse
        {
            WalletId = walletId,
            Month = month,
            Year = year,
            Total = total
        };
    }

    public async Task<DailyForecastResponse> GetDailyForecastAsync(Guid userId, Guid walletId, int month, int year)
    {
        var wallet = await GetOwnedWalletAsync(userId, walletId);

        var spentSoFar = await _entryRepository.GetSumByKindAsync(walletId, EntryKind.Diario, month, year);

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var today = DateTime.UtcNow;
        var isCurrentMonth = today.Year == year && today.Month == month;
        var isPastMonth = new DateTime(year, month, 1) < new DateTime(today.Year, today.Month, 1);

        var remainingDays = isPastMonth ? 0 : isCurrentMonth ? Math.Max(daysInMonth - today.Day, 0) : daysInMonth;

        var budgetTotal = (wallet.DailyBudget ?? 0) * daysInMonth;
        var remainingBudget = budgetTotal - spentSoFar;
        var recommendedDailySpend = remainingDays > 0 ? Math.Max(remainingBudget, 0) / remainingDays : 0;

        return new DailyForecastResponse
        {
            WalletId = walletId,
            Month = month,
            Year = year,
            DailyBudget = wallet.DailyBudget,
            SpentSoFar = spentSoFar,
            RemainingDays = remainingDays,
            RecommendedDailySpend = Math.Round(recommendedDailySpend, 2),
            IsOverBudget = wallet.DailyBudget.HasValue && spentSoFar > budgetTotal
        };
    }

    public async Task<Wallet> UpdateBudgetAsync(Guid userId, Guid walletId, UpdateWalletBudgetRequest request)
    {
        await ValidateAsync<UpdateWalletBudgetRequestValidator, UpdateWalletBudgetRequest>(request);

        var wallet = await GetOwnedWalletAsync(userId, walletId);

        await _walletRepository.UpdateBudgetAsync(walletId, request.DailyBudget);
        wallet.DailyBudget = request.DailyBudget;

        return wallet;
    }

    private async Task<Wallet> GetOwnedWalletAsync(Guid userId, Guid walletId)
    {
        return await _walletRepository.GetByIdAndUserAsync(walletId, userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));
    }

    private async Task<(decimal avgIncome, decimal avgExpenses)> CalculateMonthlyBaselineAsync(Guid walletId, int months)
    {
        var now = DateTime.UtcNow;
        decimal totalIncome = 0;
        decimal totalExpenses = 0;
        var monthsWithData = 0;

        for (var i = 0; i < months; i++)
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

    private static List<DailyBalanceEntry> BuildDailySeries(IEnumerable<Entry> entries, int month, int year)
    {
        var firstDay = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var entryList = entries.ToList();
        var runningBeforeMonth = entryList.Where(e => e.Date.Date < firstDay).Sum(SignedValue);
        var dailyNet = entryList
            .Where(e => e.Date.Date >= firstDay)
            .GroupBy(e => e.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(SignedValue));

        var running = runningBeforeMonth;
        var days = new List<DailyBalanceEntry>();

        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = firstDay.AddDays(d - 1);
            running += dailyNet.GetValueOrDefault(date, 0);
            days.Add(new DailyBalanceEntry { Date = date, Balance = Math.Round(running, 2) });
        }

        return days;
    }

    private static decimal SignedValue(Entry entry) => entry.Type == EntryType.Credit ? entry.Value : -entry.Value;

    private static DateTime EndOfMonth(int month, int year)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return new DateTime(year, month, daysInMonth, 23, 59, 59);
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
