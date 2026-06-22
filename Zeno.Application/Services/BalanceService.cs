using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Balances;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class BalanceService : IBalanceService
{
    private readonly IEntryRepository _entryRepository;
    private readonly IUserRepository _userRepository;

    public BalanceService(IEntryRepository entryRepository, IUserRepository userRepository)
    {
        _entryRepository = entryRepository;
        _userRepository = userRepository;
    }

    public async Task<BalancesResponse> GetMonthlyBalances(Guid userId, int month, int year)
    {
        var dailyBudget = await GetDailyBudget(userId);

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var today = DateTime.UtcNow.Date;

        var recurringTemplates = (await _entryRepository.GetRecurringBeforeAsync(userId, monthEnd)).ToList();

        var balance = await _entryRepository.GetSignedBalanceBeforeAsync(userId, monthStart);
        balance += RecurringEntryProjector.SumSignedBefore(recurringTemplates, monthStart);

        var entriesByDay = await GetEntriesByDay(userId, recurringTemplates, monthStart, monthEnd);

        var days = BuildMonthDays(month, year, dailyBudget, today, entriesByDay, ref balance);

        return new BalancesResponse { Month = month, Year = year, Days = days };
    }

    public async Task<BalancesHorizonResponse> GetYearlyBalances(Guid userId, int year)
    {
        var dailyBudget = await GetDailyBudget(userId);

        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = yearStart.AddYears(1);
        var today = DateTime.UtcNow.Date;

        var recurringTemplates = (await _entryRepository.GetRecurringBeforeAsync(userId, yearEnd)).ToList();

        var balance = await _entryRepository.GetSignedBalanceBeforeAsync(userId, yearStart);
        balance += RecurringEntryProjector.SumSignedBefore(recurringTemplates, yearStart);

        var entriesByDay = await GetEntriesByDay(userId, recurringTemplates, yearStart, yearEnd);

        var months = new List<BalancesResponse>();
        for (var month = 1; month <= 12; month++)
        {
            var days = BuildMonthDays(month, year, dailyBudget, today, entriesByDay, ref balance);
            months.Add(new BalancesResponse { Month = month, Year = year, Days = days });
        }

        return new BalancesHorizonResponse { Year = year, Months = months };
    }

    private async Task<decimal> GetDailyBudget(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.DailyBudget ?? 0m;
    }

    private async Task<Dictionary<DateTime, List<Entry>>> GetEntriesByDay(
        Guid userId,
        List<Entry> recurringTemplates,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var rangeEntries = await _entryRepository.GetByUserInRangeAsync(userId, rangeStart, rangeEnd);
        var recurringOccurrences = RecurringEntryProjector.ExpandOccurrencesInRange(recurringTemplates, rangeStart, rangeEnd);
        return rangeEntries.Concat(recurringOccurrences).GroupBy(e => e.Date.Date).ToDictionary(g => g.Key, g => g.ToList());
    }

    private static List<BalanceDayResponse> BuildMonthDays(
        int month,
        int year,
        decimal dailyBudget,
        DateTime today,
        Dictionary<DateTime, List<Entry>> entriesByDay,
        ref decimal balance)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var days = new List<BalanceDayResponse>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var isProjected = date > today;

            decimal entrada = 0, saida = 0, diario = 0, economia = 0, cartao = 0;

            if (entriesByDay.TryGetValue(date, out var dayEntries))
            {
                entrada = dayEntries.Where(e => e.Kind == EntryKind.Entrada).Sum(e => e.Value);
                saida = dayEntries.Where(e => e.Kind == EntryKind.Saida).Sum(e => e.Value);
                diario = dayEntries.Where(e => e.Kind == EntryKind.Diario).Sum(e => e.Value);
                economia = dayEntries.Where(e => e.Kind == EntryKind.Economia).Sum(e => e.Value);
                cartao = dayEntries.Where(e => e.Kind == EntryKind.Cartao).Sum(e => e.Value);
            }

            if (isProjected && diario == 0)
                diario = dailyBudget;

            balance += entrada - saida - diario - economia - cartao;

            days.Add(new BalanceDayResponse
            {
                Day = day,
                Entrada = entrada,
                Saida = saida,
                Diario = diario,
                Economia = economia,
                Cartao = cartao,
                Balance = balance,
                IsProjected = isProjected,
                IsToday = date == today
            });
        }

        return days;
    }
}
