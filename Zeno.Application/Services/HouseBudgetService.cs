using FluentValidation;
using FluentValidation.Results;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Houses;
using Zeno.Application.Responses.Houses;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using HouseEntity = Zeno.Domain.House.House;

namespace Zeno.Application.Services;

public class HouseBudgetService : IHouseBudgetService
{
    private const decimal NeedsShare = 0.5m;
    private const decimal WantsShare = 0.3m;
    private const int MaxGoalMonths = 36;

    private readonly IValidator<SaveHouseGoalRequest> _goalValidator;
    private readonly IHouseRepository _houseRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;

    public HouseBudgetService(
        IValidator<SaveHouseGoalRequest> goalValidator,
        IHouseRepository houseRepository,
        IEntryRepository entryRepository,
        IUserRepository userRepository,
        IClock clock)
    {
        _goalValidator = goalValidator;
        _houseRepository = houseRepository;
        _entryRepository = entryRepository;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<HouseBudgetResponse> GetBudgetAsync(Guid userId, Guid houseId, int? month, int? year)
    {
        var house = await GetAccessibleHouseAsync(userId, houseId);
        var today = LocalToday();

        var selectedMonth = month ?? today.Month;
        var selectedYear = year ?? today.Year;
        if (selectedMonth is < 1 or > 12 || selectedYear is < 2000 or > 2100)
            throw Invalid("month", "Mês ou ano inválido.");

        var residents = ResidentIds(house);
        var selectedStart = new DateTime(selectedYear, selectedMonth, 1);

        // One query range covering the selected month and, when there is a goal, every month since its start.
        var rangeStart = selectedStart;
        var rangeEnd = selectedStart.AddMonths(1);
        DateTime? goalFirstMonth = null;
        if (HasGoal(house))
        {
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var goalStart = house.GoalStartDate!.Value;
            var startMonth = new DateTime(goalStart.Year, goalStart.Month, 1);
            var earliest = currentMonth.AddMonths(-(MaxGoalMonths - 1));
            goalFirstMonth = startMonth < earliest ? earliest : startMonth;

            if (goalFirstMonth < rangeStart) rangeStart = goalFirstMonth.Value;
            if (currentMonth.AddMonths(1) > rangeEnd) rangeEnd = currentMonth.AddMonths(1);
        }

        var incomeByMonth = await PooledIncomeByMonthAsync(residents, rangeStart, rangeEnd);
        var totalIncome = IncomeOf(incomeByMonth, selectedStart);

        var needs = RoundMoney(totalIncome * NeedsShare);
        var wants = RoundMoney(totalIncome * WantsShare);
        var savings = SavingsOf(totalIncome);
        var owner = await _userRepository.GetByIdAsync(house.UserId);

        return new HouseBudgetResponse
        {
            HouseId = house.Id,
            Month = selectedMonth,
            Year = selectedYear,
            Currency = owner?.Currency ?? Currency.BRL,
            ResidentCount = residents.Count,
            IsOwner = house.UserId == userId,
            TotalIncome = totalIncome,
            Needs = needs,
            Wants = wants,
            Savings = savings,
            FreePerPerson = RoundMoney(wants / residents.Count),
            Goal = goalFirstMonth is null ? null : BuildGoal(house, incomeByMonth, goalFirstMonth.Value, today, savings)
        };
    }

    public async Task<HouseGoalResponse?> SaveGoalAsync(Guid userId, Guid houseId, SaveHouseGoalRequest request)
    {
        var validation = await _goalValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var house = await GetOwnedHouseAsync(userId, houseId);
        var today = LocalToday();

        var isNew = !HasGoal(house);
        house.GoalName = request.Name.Trim();
        house.GoalTargetAmount = request.TargetAmount;
        if (isNew)
            house.GoalStartDate = new DateTime(today.Year, today.Month, 1);

        await _houseRepository.UpdateAsync(house);

        var budget = await GetBudgetAsync(userId, houseId, null, null);
        return budget.Goal;
    }

    public async Task DeleteGoalAsync(Guid userId, Guid houseId)
    {
        var house = await GetOwnedHouseAsync(userId, houseId);

        house.GoalName = string.Empty;
        house.GoalTargetAmount = null;
        house.GoalStartDate = null;

        await _houseRepository.UpdateAsync(house);
    }

    private static HouseGoalResponse BuildGoal(
        HouseEntity house,
        IReadOnlyDictionary<(int Year, int Month), decimal> incomeByMonth,
        DateTime firstMonth,
        DateTime today,
        decimal monthlyContribution)
    {
        var target = house.GoalTargetAmount!.Value;
        var currentMonth = new DateTime(today.Year, today.Month, 1);

        decimal accumulated = 0m;
        for (var m = firstMonth; m <= currentMonth; m = m.AddMonths(1))
            accumulated += SavingsOf(IncomeOf(incomeByMonth, m));

        var progress = target > 0 ? Math.Min(Math.Round(accumulated / target * 100m, 1), 100m) : 0m;

        int? monthsRemaining = null;
        if (accumulated >= target)
            monthsRemaining = 0;
        else if (monthlyContribution > 0)
            monthsRemaining = (int)Math.Ceiling((target - accumulated) / monthlyContribution);

        return new HouseGoalResponse
        {
            Name = house.GoalName,
            TargetAmount = target,
            StartDate = house.GoalStartDate!.Value,
            AccumulatedAmount = accumulated,
            ProgressPercent = Math.Max(progress, 0m),
            MonthlyContribution = monthlyContribution,
            MonthsRemaining = monthsRemaining
        };
    }

    /// <summary>Soma, por mês, as entradas (inclusive recorrências) de todos os moradores.</summary>
    private async Task<Dictionary<(int Year, int Month), decimal>> PooledIncomeByMonthAsync(
        IReadOnlyCollection<Guid> residents, DateTime rangeStart, DateTime rangeEnd)
    {
        var totals = new Dictionary<(int Year, int Month), decimal>();

        foreach (var residentId in residents)
        {
            var entries = await _entryRepository.GetByUserInRangeAsync(residentId, rangeStart, rangeEnd);
            var templates = await _entryRepository.GetRecurringBeforeAsync(residentId, rangeEnd);
            var occurrences = RecurringEntryProjector.ExpandOccurrencesInRange(templates, rangeStart, rangeEnd);

            foreach (var entry in entries.Concat(occurrences).Where(e => e.Kind == EntryKind.Entrada))
            {
                var key = (entry.Date.Year, entry.Date.Month);
                totals[key] = totals.GetValueOrDefault(key) + entry.Value;
            }
        }

        return totals;
    }

    private static decimal IncomeOf(IReadOnlyDictionary<(int Year, int Month), decimal> byMonth, DateTime month)
    {
        return byMonth.TryGetValue((month.Year, month.Month), out var value) ? value : 0m;
    }

    private async Task<HouseEntity> GetAccessibleHouseAsync(Guid userId, Guid houseId)
    {
        var house = await _houseRepository.GetByIdAsync(houseId);
        if (house is null || (house.UserId != userId && house.Members.All(m => m.UserId != userId)))
            throw Invalid(nameof(houseId), "Casa não encontrada.");

        return house;
    }

    private async Task<HouseEntity> GetOwnedHouseAsync(Guid userId, Guid houseId)
    {
        var house = await _houseRepository.GetByIdAsync(houseId);
        if (house is null || house.UserId != userId)
            throw Invalid(nameof(houseId), "Casa não encontrada.");

        return house;
    }

    private static List<Guid> ResidentIds(HouseEntity house)
    {
        return house.Members.Select(m => m.UserId).Append(house.UserId).Distinct().ToList();
    }

    private static bool HasGoal(HouseEntity house)
    {
        return house.GoalTargetAmount.HasValue && house.GoalStartDate.HasValue;
    }

    private DateTime LocalToday()
    {
        var timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(NotificationPreference.DefaultTimeZoneId, out var tz)
            ? tz
            : TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, timeZone).Date;
    }

    /// <summary>O que sobra do total depois das fatias de 50% e 30% (os 20% da meta, sem perder centavos).</summary>
    private static decimal SavingsOf(decimal totalIncome)
    {
        return totalIncome - RoundMoney(totalIncome * NeedsShare) - RoundMoney(totalIncome * WantsShare);
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static AppValidationException Invalid(string property, string message)
    {
        return new AppValidationException(new ValidationResult(new List<ValidationFailure>
        {
            new(property, message)
        }));
    }
}
