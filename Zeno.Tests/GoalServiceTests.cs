using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Requests.Goals;
using Zeno.Application.Services;
using Zeno.Application.Validators;
using Zeno.Domain.Enum;
using Zeno.Domain.Goals;
using Zeno.Domain.Interfaces;
using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Tests;

public class GoalServiceTests
{
    private readonly Mock<IGoalRepository> _goalRepo = new();
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly Guid _userId = Guid.NewGuid();

    public GoalServiceTests()
    {
        _goalRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>())).ReturnsAsync((Goal?)null);
        _goalRepo.Setup(r => r.UpsertAsync(It.IsAny<Goal>())).ReturnsAsync((Goal g) => g);
        _entryRepo.Setup(r => r.GetByUserInRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Array.Empty<EntryEntity>());
        _entryRepo.Setup(r => r.GetRecurringBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<EntryEntity>());
    }

    private GoalService CreateService() =>
        new(new SaveGoalRequestValidator(), _goalRepo.Object, _entryRepo.Object);

    private static SaveGoalRequest ValidRequest(decimal target = 100_000m, decimal initial = 0m) => new()
    {
        Name = "Reserva de emergência",
        TargetAmount = target,
        MonthlyContribution = 500m,
        InitialAmount = initial,
        AnnualRatePercent = 14.55
    };

    [Fact]
    public async Task GetAsync_WithoutGoal_ReturnsNull()
    {
        var result = await CreateService().GetAsync(_userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_NewGoal_StartsToday_AndStoresFields()
    {
        var result = await CreateService().SaveAsync(_userId, ValidRequest());

        Assert.Equal("Reserva de emergência", result.Name);
        Assert.Equal(100_000m, result.TargetAmount);
        Assert.Equal(500m, result.MonthlyContribution);
        Assert.Equal(DateTime.UtcNow.Date, result.StartDate);
        Assert.Equal(0m, result.SavedAmount);
        Assert.Equal(0m, result.ProgressPercent);
        _goalRepo.Verify(r => r.UpsertAsync(It.Is<Goal>(g => g.UserId == _userId)), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingGoal_KeepsIdAndStartDate()
    {
        var existing = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Name = "Antiga",
            TargetAmount = 10m,
            MonthlyContribution = 1m,
            StartDate = new DateTime(2026, 1, 1)
        };
        _goalRepo.Setup(r => r.GetByUserAsync(_userId)).ReturnsAsync(existing);

        var result = await CreateService().SaveAsync(_userId, ValidRequest());

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(new DateTime(2026, 1, 1), result.StartDate);
        Assert.Equal("Reserva de emergência", result.Name);
    }

    [Fact]
    public async Task SavedAmount_IsInitialPlusEconomiaEntriesOnly()
    {
        _entryRepo.Setup(r => r.GetByUserInRangeAsync(_userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new[]
            {
                new EntryEntity { Kind = EntryKind.Economia, Value = 500m, Date = DateTime.UtcNow.Date },
                new EntryEntity { Kind = EntryKind.Saida, Value = 100m, Date = DateTime.UtcNow.Date },
            });

        var result = await CreateService().SaveAsync(_userId, ValidRequest(initial: 1000m));

        Assert.Equal(1500m, result.SavedAmount);
        Assert.Equal(1.5m, result.ProgressPercent);
    }

    [Fact]
    public async Task ProgressPercent_IsCappedAt100()
    {
        var result = await CreateService().SaveAsync(_userId, ValidRequest(target: 1000m, initial: 2000m));

        Assert.Equal(100m, result.ProgressPercent);
    }

    [Theory]
    [InlineData("", 1000, 100, 0, 10)]
    [InlineData("Meta", 0, 100, 0, 10)]
    [InlineData("Meta", 1000, 0, 0, 10)]
    [InlineData("Meta", 1000, 100, -1, 10)]
    [InlineData("Meta", 1000, 100, 0, 101)]
    public async Task SaveAsync_InvalidRequest_Throws(string name, decimal target, decimal monthly, decimal initial, double rate)
    {
        var request = new SaveGoalRequest
        {
            Name = name,
            TargetAmount = target,
            MonthlyContribution = monthly,
            InitialAmount = initial,
            AnnualRatePercent = rate
        };

        await Assert.ThrowsAsync<AppValidationException>(() => CreateService().SaveAsync(_userId, request));
        _goalRepo.Verify(r => r.UpsertAsync(It.IsAny<Goal>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DeletesByUser()
    {
        await CreateService().DeleteAsync(_userId);

        _goalRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
    }
}
