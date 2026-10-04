using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Houses;
using Zeno.Application.Responses.Houses;
using Zeno.Application.Services;
using Zeno.Application.Validators;
using Zeno.Domain.Enum;
using Zeno.Domain.House;
using Zeno.Domain.Interfaces;
using EntryEntity = Zeno.Domain.Entry.Entry;
using HouseEntity = Zeno.Domain.House.House;
using UserEntity = Zeno.Domain.User.User;

namespace Zeno.Tests;

public class HouseBudgetServiceTests
{
    private readonly Mock<IHouseRepository> _houseRepo = new();
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly FakeClock _clock = new();

    private readonly Dictionary<Guid, List<EntryEntity>> _entries = new();
    private readonly Dictionary<Guid, List<EntryEntity>> _recurring = new();
    private readonly List<EntryEntity> _houseRecurring = new();

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _member1 = Guid.NewGuid();
    private readonly Guid _member2 = Guid.NewGuid();
    private readonly HouseEntity _house;

    public HouseBudgetServiceTests()
    {
        // Oct 15 2026, midday: no time-zone ambiguity.
        _clock.UtcNow = new DateTime(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

        _house = new HouseEntity
        {
            Id = Guid.NewGuid(),
            UserId = _ownerId,
            Name = "Casa",
            Members = new List<HouseMember>
            {
                new() { UserId = _member1, Name = "A", Email = "a@x.com" },
                new() { UserId = _member2, Name = "B", Email = "b@x.com" },
            }
        };

        _houseRepo.Setup(r => r.GetByIdAsync(_house.Id)).ReturnsAsync(() => _house);
        _houseRepo.Setup(r => r.UpdateAsync(It.IsAny<HouseEntity>())).ReturnsAsync((HouseEntity h) => h);
        _userRepo.Setup(r => r.GetByIdAsync(_ownerId)).ReturnsAsync(new UserEntity { Id = _ownerId, Currency = Currency.USD });

        _entryRepo.Setup(r => r.GetByUserInRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync((Guid u, DateTime? _, DateTime? _) =>
                (IEnumerable<EntryEntity>)(_entries.TryGetValue(u, out var l) ? l.ToList() : new List<EntryEntity>()));
        _entryRepo.Setup(r => r.GetRecurringByHouseAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(() => (IEnumerable<EntryEntity>)_houseRecurring.ToList());
        _entryRepo.Setup(r => r.GetRecurringBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync((Guid u, DateTime _) =>
                (IEnumerable<EntryEntity>)(_recurring.TryGetValue(u, out var l) ? l.ToList() : new List<EntryEntity>()));
    }

    private HouseBudgetService CreateService() =>
        new(new SaveHouseGoalRequestValidator(), _houseRepo.Object, _entryRepo.Object, _userRepo.Object, _clock);

    private void AddEntry(Guid userId, EntryKind kind, decimal value, int month, int day = 5, int year = 2026)
    {
        if (!_entries.TryGetValue(userId, out var list))
            _entries[userId] = list = new List<EntryEntity>();
        list.Add(new EntryEntity { UserId = userId, Kind = kind, Value = value, Date = new DateTime(year, month, day) });
    }

    private void AddMonthlyIncome(decimal owner, decimal member1, decimal member2)
    {
        AddEntry(_ownerId, EntryKind.Entrada, owner, 10);
        AddEntry(_member1, EntryKind.Entrada, member1, 10);
        AddEntry(_member2, EntryKind.Entrada, member2, 10);
    }

    [Fact]
    public async Task Budget_SumsIncomeOfOwnerAndMembers_AndIgnoresOtherKinds()
    {
        AddMonthlyIncome(3000m, 2000m, 1000m);
        AddEntry(_ownerId, EntryKind.Saida, 500m, 10);
        AddEntry(_member2, EntryKind.Diario, 50m, 10);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(6000m, result.TotalIncome);
        Assert.Equal(3000m, result.Needs);
        Assert.Equal(1800m, result.Wants);
        Assert.Equal(1200m, result.Savings);
        Assert.Equal(3, result.ResidentCount);
        Assert.Equal(600m, result.FreePerPerson);
        Assert.Equal(10, result.Month);
        Assert.Equal(2026, result.Year);
        Assert.Equal(Currency.USD, result.Currency);
        Assert.True(result.IsOwner);
        Assert.Null(result.Goal);
    }

    [Fact]
    public async Task Budget_AMemberCanViewIt_ButIsNotMarkedAsOwner()
    {
        AddMonthlyIncome(1000m, 0m, 0m);

        var result = await CreateService().GetBudgetAsync(_member1, _house.Id, null, null);

        Assert.False(result.IsOwner);
        Assert.Equal(1000m, result.TotalIncome);
    }

    [Fact]
    public async Task Budget_ForSomeoneOutsideTheHouse_Throws()
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateService().GetBudgetAsync(Guid.NewGuid(), _house.Id, null, null));
        await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateService().GetBudgetAsync(_ownerId, Guid.NewGuid(), null, null));
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(5, 1999)]
    public async Task Budget_InvalidMonthOrYear_Throws(int month, int year)
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateService().GetBudgetAsync(_ownerId, _house.Id, month, year));
    }

    [Fact]
    public async Task Budget_UsesTheRequestedMonth()
    {
        AddEntry(_ownerId, EntryKind.Entrada, 1000m, 9);
        AddEntry(_ownerId, EntryKind.Entrada, 4000m, 10);

        var september = await CreateService().GetBudgetAsync(_ownerId, _house.Id, 9, 2026);

        Assert.Equal(1000m, september.TotalIncome);
        Assert.Equal(9, september.Month);
    }

    [Fact]
    public async Task Budget_CountsRecurringIncome()
    {
        _recurring[_member1] = new List<EntryEntity>
        {
            new() { UserId = _member1, Kind = EntryKind.Entrada, Value = 1000m, IsRecurring = true, Date = new DateTime(2026, 8, 5) }
        };

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(1000m, result.TotalIncome);
    }

    [Fact]
    public async Task Budget_RoundsToCents_AndSlicesAlwaysAddUpToTheTotal()
    {
        AddEntry(_ownerId, EntryKind.Entrada, 100.01m, 10);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(50.01m, result.Needs);
        Assert.Equal(30.00m, result.Wants);
        Assert.Equal(20.00m, result.Savings);
        Assert.Equal(result.TotalIncome, result.Needs + result.Wants + result.Savings);
    }

    [Fact]
    public async Task Budget_FreeAmountIsSplitEquallyAmongResidents()
    {
        AddEntry(_ownerId, EntryKind.Entrada, 1000m, 10);
        _house.Members.Clear();
        for (var i = 0; i < 6; i++)
            _house.Members.Add(new HouseMember { UserId = Guid.NewGuid() });
        // owner + 6 members = 7 residents; 30% of 1000 = 300; 300 / 7 = 42.857...

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(7, result.ResidentCount);
        Assert.Equal(42.86m, result.FreePerPerson);
    }

    [Fact]
    public async Task Budget_OwnerListedAsMemberIsNotCountedTwice()
    {
        _house.Members.Add(new HouseMember { UserId = _ownerId });

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(3, result.ResidentCount);
    }

    private EntryEntity HouseExpense(decimal value, DateTime date, EntryKind kind = EntryKind.Saida, DateTime? endsOn = null)
    {
        var entry = new EntryEntity
        {
            UserId = _ownerId,
            HouseId = _house.Id,
            Kind = kind,
            Value = value,
            IsRecurring = true,
            Date = date,
            RecurrenceEndDate = endsOn
        };
        _houseRecurring.Add(entry);
        return entry;
    }

    [Fact]
    public async Task FixedExpenses_RecurringHouseExpensesAreCountedAgainstTheNeedsSlice()
    {
        AddEntry(_ownerId, EntryKind.Entrada, 6000m, 10);
        HouseExpense(1500m, new DateTime(2026, 8, 5));   // rent, started in August
        HouseExpense(300m, new DateTime(2026, 10, 12));  // started this month

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(3000m, result.Needs);
        Assert.Equal(1800m, result.FixedExpenses);
        Assert.Equal(1200m, result.NeedsRemaining);
    }

    [Fact]
    public async Task FixedExpenses_OnlyExpenseKindsCount()
    {
        HouseExpense(1000m, new DateTime(2026, 8, 5));
        HouseExpense(900m, new DateTime(2026, 8, 5), EntryKind.Entrada);
        HouseExpense(700m, new DateTime(2026, 8, 5), EntryKind.Diario);
        HouseExpense(600m, new DateTime(2026, 8, 5), EntryKind.Economia);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(1000m, result.FixedExpenses);
    }

    [Fact]
    public async Task FixedExpenses_StopAfterTheirEndDate_AndIgnoreFutureOnes()
    {
        HouseExpense(500m, new DateTime(2026, 3, 5), endsOn: new DateTime(2026, 9, 30)); // ended before October
        HouseExpense(400m, new DateTime(2026, 11, 5));                                    // starts after October
        HouseExpense(200m, new DateTime(2026, 3, 5), endsOn: new DateTime(2026, 12, 31)); // still running

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(200m, result.FixedExpenses);
    }

    [Fact]
    public async Task FixedExpenses_OverTheNeedsSlice_GiveNegativeRemaining_AndDoNotChangeTheFreeAmount()
    {
        AddEntry(_ownerId, EntryKind.Entrada, 2000m, 10);
        HouseExpense(1500m, new DateTime(2026, 8, 5));

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(1000m, result.Needs);
        Assert.Equal(-500m, result.NeedsRemaining);
        Assert.Equal(600m, result.Wants);
        Assert.Equal(200m, result.FreePerPerson);   // 600 / 3 residents, untouched by the fixed expenses
    }

    [Fact]
    public async Task FixedExpenses_FollowTheRequestedMonth()
    {
        HouseExpense(1000m, new DateTime(2026, 8, 5));

        var july = await CreateService().GetBudgetAsync(_ownerId, _house.Id, 7, 2026);
        var august = await CreateService().GetBudgetAsync(_ownerId, _house.Id, 8, 2026);
        var november = await CreateService().GetBudgetAsync(_ownerId, _house.Id, 11, 2026);

        Assert.Equal(0m, july.FixedExpenses);
        Assert.Equal(1000m, august.FixedExpenses);
        Assert.Equal(1000m, november.FixedExpenses);
    }

    [Fact]
    public async Task FixedExpenses_WithoutAny_AreZero()
    {
        AddEntry(_ownerId, EntryKind.Entrada, 1000m, 10);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(0m, result.FixedExpenses);
        Assert.Equal(result.Needs, result.NeedsRemaining);
    }

    [Fact]
    public void BudgetResponse_ExposesNoPerResidentData()
    {
        var collections = typeof(HouseBudgetResponse).GetProperties()
            .Where(p => p.PropertyType != typeof(string)
                        && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType));

        Assert.Empty(collections);
    }

    [Fact]
    public async Task Goal_AccumulatesTwentyPercentOfEachMonthSinceStart()
    {
        _house.GoalName = "Viagem";
        _house.GoalTargetAmount = 12000m;
        _house.GoalStartDate = new DateTime(2026, 8, 1);
        AddEntry(_ownerId, EntryKind.Entrada, 1000m, 8);
        AddEntry(_member1, EntryKind.Entrada, 2000m, 9);
        AddEntry(_member2, EntryKind.Entrada, 3000m, 10);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.NotNull(result.Goal);
        Assert.Equal("Viagem", result.Goal!.Name);
        Assert.Equal(12000m, result.Goal.TargetAmount);
        Assert.Equal(1200m, result.Goal.AccumulatedAmount);   // 200 + 400 + 600
        Assert.Equal(10.0m, result.Goal.ProgressPercent);
        Assert.Equal(600m, result.Goal.MonthlyContribution);  // 20% of October (3000)
        Assert.Equal(18, result.Goal.MonthsRemaining);        // (12000 - 1200) / 600
    }

    [Fact]
    public async Task Goal_ReachedIsCappedAt100_WithZeroMonthsRemaining()
    {
        _house.GoalName = "Meta";
        _house.GoalTargetAmount = 500m;
        _house.GoalStartDate = new DateTime(2026, 10, 1);
        AddEntry(_ownerId, EntryKind.Entrada, 5000m, 10);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(100m, result.Goal!.ProgressPercent);
        Assert.Equal(0, result.Goal.MonthsRemaining);
    }

    [Fact]
    public async Task Goal_WithNoIncome_HasNoEstimate()
    {
        _house.GoalName = "Meta";
        _house.GoalTargetAmount = 5000m;
        _house.GoalStartDate = new DateTime(2026, 10, 1);

        var result = await CreateService().GetBudgetAsync(_ownerId, _house.Id, null, null);

        Assert.Equal(0m, result.Goal!.AccumulatedAmount);
        Assert.Null(result.Goal.MonthsRemaining);
    }

    [Fact]
    public async Task SaveGoal_ByOwner_StoresTrimmedNameTargetAndStartMonth()
    {
        var result = await CreateService().SaveGoalAsync(_ownerId, _house.Id, new SaveHouseGoalRequest
        {
            Name = "  Reforma  ",
            TargetAmount = 20000m
        });

        _houseRepo.Verify(r => r.UpdateAsync(It.Is<HouseEntity>(h =>
            h.GoalName == "Reforma" && h.GoalTargetAmount == 20000m && h.GoalStartDate == new DateTime(2026, 10, 1))), Times.Once);
        Assert.Equal("Reforma", result!.Name);
    }

    [Fact]
    public async Task SaveGoal_WhenGoalExists_KeepsTheOriginalStartDate()
    {
        _house.GoalName = "Antiga";
        _house.GoalTargetAmount = 100m;
        _house.GoalStartDate = new DateTime(2026, 3, 1);

        await CreateService().SaveGoalAsync(_ownerId, _house.Id, new SaveHouseGoalRequest { Name = "Nova", TargetAmount = 900m });

        Assert.Equal(new DateTime(2026, 3, 1), _house.GoalStartDate);
        Assert.Equal("Nova", _house.GoalName);
        Assert.Equal(900m, _house.GoalTargetAmount);
    }

    [Fact]
    public async Task SaveGoal_ByAMember_Throws()
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateService().SaveGoalAsync(_member1, _house.Id, new SaveHouseGoalRequest { Name = "X", TargetAmount = 10m }));
        _houseRepo.Verify(r => r.UpdateAsync(It.IsAny<HouseEntity>()), Times.Never);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("Meta", 0)]
    [InlineData("Meta", -5)]
    public async Task SaveGoal_InvalidRequest_Throws(string name, decimal target)
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateService().SaveGoalAsync(_ownerId, _house.Id, new SaveHouseGoalRequest { Name = name, TargetAmount = target }));
    }

    [Fact]
    public async Task DeleteGoal_ByOwner_ClearsIt_ButAMemberCannot()
    {
        _house.GoalName = "Meta";
        _house.GoalTargetAmount = 100m;
        _house.GoalStartDate = new DateTime(2026, 10, 1);
        var service = CreateService();

        await Assert.ThrowsAsync<AppValidationException>(() => service.DeleteGoalAsync(_member1, _house.Id));
        Assert.NotNull(_house.GoalTargetAmount);

        await service.DeleteGoalAsync(_ownerId, _house.Id);

        Assert.Equal(string.Empty, _house.GoalName);
        Assert.Null(_house.GoalTargetAmount);
        Assert.Null(_house.GoalStartDate);
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
