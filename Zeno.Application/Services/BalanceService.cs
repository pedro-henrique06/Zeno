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
        var user = await _userRepository.GetByIdAsync(userId);
        var dailyBudget = user?.DailyBudget ?? 0m;

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var today = DateTime.UtcNow.Date;

        var priorEntries = await _entryRepository.GetByUserInRangeAsync(userId, null, monthStart);
        var balance = priorEntries.Sum(SignedValue);

        var monthEntries = await _entryRepository.GetByUserInRangeAsync(userId, monthStart, monthEnd);
        var entriesByDay = monthEntries.GroupBy(e => e.Date.Date).ToDictionary(g => g.Key, g => g.ToList());

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var days = new List<BalanceDayResponse>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var isProjected = date > today;

            decimal entrada = 0, saida = 0, diario = 0, economia = 0, cartao = 0;

            if (!isProjected && entriesByDay.TryGetValue(date, out var dayEntries))
            {
                entrada = dayEntries.Where(e => e.Kind == EntryKind.Entrada).Sum(e => e.Value);
                saida = dayEntries.Where(e => e.Kind == EntryKind.Saida).Sum(e => e.Value);
                diario = dayEntries.Where(e => e.Kind == EntryKind.Diario).Sum(e => e.Value);
                economia = dayEntries.Where(e => e.Kind == EntryKind.Economia).Sum(e => e.Value);
                cartao = dayEntries.Where(e => e.Kind == EntryKind.Cartao).Sum(e => e.Value);
            }

            if (isProjected)
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

        return new BalancesResponse { Month = month, Year = year, Days = days };
    }

    private static decimal SignedValue(Entry entry) => entry.Kind switch
    {
        EntryKind.Entrada => entry.Value,
        _ => -entry.Value
    };
}
