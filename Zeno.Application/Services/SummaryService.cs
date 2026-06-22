using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Summary;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class SummaryService : ISummaryService
{
    private readonly IEntryRepository _entryRepository;
    private readonly IUserRepository _userRepository;

    public SummaryService(IEntryRepository entryRepository, IUserRepository userRepository)
    {
        _entryRepository = entryRepository;
        _userRepository = userRepository;
    }

    public async Task<SummaryResponse> GetMonthlySummary(Guid userId, int month, int year)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var dailyBudget = user?.DailyBudget ?? 0m;

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var today = DateTime.UtcNow.Date;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        int elapsedDays;
        if (monthEnd <= today)
            elapsedDays = daysInMonth;
        else if (monthStart > today)
            elapsedDays = 0;
        else
            elapsedDays = today.Day;

        var remainingDays = daysInMonth - elapsedDays;

        var monthEntries = await _entryRepository.GetByUserInRangeAsync(userId, monthStart, monthEnd);
        var recurringTemplates = await _entryRepository.GetRecurringBeforeAsync(userId, monthEnd);
        var recurringOccurrences = RecurringEntryProjector.ExpandOccurrencesInRange(recurringTemplates, monthStart, monthEnd);
        monthEntries = monthEntries.Concat(recurringOccurrences);

        var totalEntrada = monthEntries.Where(e => e.Kind == EntryKind.Entrada).Sum(e => e.Value);
        var totalSaida = monthEntries.Where(e => e.Kind == EntryKind.Saida).Sum(e => e.Value);
        var totalDiario = monthEntries.Where(e => e.Kind == EntryKind.Diario).Sum(e => e.Value);
        var totalEconomia = monthEntries.Where(e => e.Kind == EntryKind.Economia).Sum(e => e.Value);
        var totalCartao = monthEntries.Where(e => e.Kind == EntryKind.Cartao).Sum(e => e.Value);

        var projectedRemainingDaily = dailyBudget * remainingDays;
        var costOfLiving = totalSaida + totalDiario + totalCartao + projectedRemainingDaily;
        var performance = totalEntrada - totalSaida - totalDiario - totalCartao - projectedRemainingDaily;
        var economizedPercent = totalEntrada > 0 ? totalEconomia / totalEntrada * 100 : 0;
        var dailyAverageReal = elapsedDays > 0 ? totalDiario / elapsedDays : 0;

        return new SummaryResponse
        {
            Performance = performance,
            EconomizedPercent = economizedPercent,
            CostOfLiving = costOfLiving,
            DailyAverageReal = dailyAverageReal,
            DailyBudget = dailyBudget,
            DaysElapsed = elapsedDays,
            DaysRemaining = remainingDays,
            DaysInMonth = daysInMonth,
            Movements = new MovementsResponse
            {
                Entrada = totalEntrada,
                Saida = totalSaida,
                Diario = totalDiario,
                Economia = totalEconomia,
                Cartao = totalCartao
            }
        };
    }

    public async Task<EconomizedHorizonResponse> GetEconomizedHorizon(Guid userId, int year)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = yearStart.AddYears(1);

        var yearEntries = await _entryRepository.GetByUserInRangeAsync(userId, yearStart, yearEnd);
        var recurringTemplates = await _entryRepository.GetRecurringBeforeAsync(userId, yearEnd);
        var recurringOccurrences = RecurringEntryProjector.ExpandOccurrencesInRange(recurringTemplates, yearStart, yearEnd);
        yearEntries = yearEntries.Concat(recurringOccurrences).ToList();

        var months = new List<EconomizedMonthResponse>();
        for (var month = 1; month <= 12; month++)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var monthEntries = yearEntries.Where(e => e.Date >= monthStart && e.Date < monthEnd);

            var entrada = monthEntries.Where(e => e.Kind == EntryKind.Entrada).Sum(e => e.Value);
            var economia = monthEntries.Where(e => e.Kind == EntryKind.Economia).Sum(e => e.Value);

            months.Add(new EconomizedMonthResponse
            {
                Month = month,
                EconomizedPercent = entrada > 0 ? economia / entrada * 100 : 0,
                Economia = economia,
                Entrada = entrada
            });
        }

        var totalEntrada = months.Sum(m => m.Entrada);
        var totalEconomia = months.Sum(m => m.Economia);

        return new EconomizedHorizonResponse
        {
            Year = year,
            EconomizedPercent = totalEntrada > 0 ? totalEconomia / totalEntrada * 100 : 0,
            Economia = totalEconomia,
            Entrada = totalEntrada,
            Months = months
        };
    }
}
