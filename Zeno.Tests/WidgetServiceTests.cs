using Moq;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Goals;
using Zeno.Application.Responses.Summary;
using Zeno.Application.Services;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Widgets;
using EntryEntity = Zeno.Domain.Entry.Entry;
using UserEntity = Zeno.Domain.User.User;

namespace Zeno.Tests;

public class WidgetServiceTests
{
    private readonly Mock<IWidgetKeyRepository> _keyRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISummaryService> _summary = new();
    private readonly Mock<IGoalService> _goals = new();
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly FakeClock _clock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public WidgetServiceTests()
    {
        _keyRepo.Setup(r => r.UpsertAsync(It.IsAny<WidgetKey>())).ReturnsAsync((WidgetKey k) => k);
        _keyRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>())).ReturnsAsync((WidgetKey?)null);
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(new UserEntity
        {
            Id = _userId,
            Currency = Currency.BRL,
            Language = Language.PtBR
        });
        _summary.Setup(s => s.GetMonthlySummary(_userId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new SummaryResponse
            {
                Performance = 2480m,
                DailyBudget = 100m,
                Movements = new MovementsResponse { Entrada = 6200m, Saida = 1000m, Diario = 400m, Cartao = 50m }
            });
        _goals.Setup(g => g.GetAsync(_userId)).ReturnsAsync((GoalResponse?)null);
        _entryRepo.Setup(r => r.GetByUserInRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Array.Empty<EntryEntity>());
        _entryRepo.Setup(r => r.GetRecurringBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<EntryEntity>());
    }

    private WidgetService CreateService() =>
        new(_keyRepo.Object, _userRepo.Object, _summary.Object, _goals.Object, _entryRepo.Object, _clock);

    [Fact]
    public async Task CreateKey_ReturnsPrefixedKey_AndStoresOnlyItsHash()
    {
        WidgetKey? saved = null;
        _keyRepo.Setup(r => r.UpsertAsync(It.IsAny<WidgetKey>()))
            .Callback<WidgetKey>(k => saved = k)
            .ReturnsAsync((WidgetKey k) => k);

        var result = await CreateService().CreateKeyAsync(_userId);

        Assert.StartsWith("zw_", result.Key);
        Assert.NotNull(saved);
        Assert.Equal(_userId, saved!.UserId);
        Assert.NotEqual(result.Key, saved.KeyHash);
        Assert.DoesNotContain(result.Key, saved.KeyHash);
    }

    [Fact]
    public async Task CreateKey_TwiceForSameUser_ProducesDifferentKeys_AndKeepsRecordId()
    {
        WidgetKey? existing = null;
        _keyRepo.Setup(r => r.GetByUserAsync(_userId)).ReturnsAsync(() => existing);
        _keyRepo.Setup(r => r.UpsertAsync(It.IsAny<WidgetKey>()))
            .Callback<WidgetKey>(k => existing = k)
            .ReturnsAsync((WidgetKey k) => k);
        var service = CreateService();

        var first = await service.CreateKeyAsync(_userId);
        var firstId = existing!.Id;
        var second = await service.CreateKeyAsync(_userId);

        Assert.NotEqual(first.Key, second.Key);
        Assert.Equal(firstId, existing.Id);
    }

    [Fact]
    public async Task GetSummary_WithoutKeyOrWithUnknownKey_ReturnsNull()
    {
        _keyRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((WidgetKey?)null);
        var service = CreateService();

        Assert.Null(await service.GetSummaryAsync(null, null));
        Assert.Null(await service.GetSummaryAsync("  ", null));
        Assert.Null(await service.GetSummaryAsync("zw_unknown", null));
    }

    [Fact]
    public async Task GetSummary_ValidKey_UsesLocalDay_AndComputesAvailableToday()
    {
        // 02:00 UTC on Oct 4 is still Oct 3 (23:00) in America/Sao_Paulo.
        _clock.UtcNow = new DateTime(2026, 10, 4, 2, 0, 0, DateTimeKind.Utc);

        string? storedHash = null;
        _keyRepo.Setup(r => r.UpsertAsync(It.IsAny<WidgetKey>()))
            .Callback<WidgetKey>(k => storedHash = k.KeyHash)
            .ReturnsAsync((WidgetKey k) => k);
        var service = CreateService();
        var created = await service.CreateKeyAsync(_userId);
        _keyRepo.Setup(r => r.GetByHashAsync(storedHash!))
            .ReturnsAsync(new WidgetKey { Id = Guid.NewGuid(), UserId = _userId, KeyHash = storedHash! });

        _entryRepo.Setup(r => r.GetByUserInRangeAsync(_userId, new DateTime(2026, 10, 3), new DateTime(2026, 10, 4)))
            .ReturnsAsync(new[]
            {
                new EntryEntity { Kind = EntryKind.Diario, Value = 30m },
                new EntryEntity { Kind = EntryKind.Saida, Value = 999m },
            });

        var result = await service.GetSummaryAsync(created.Key, null);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Month);
        Assert.Equal(2026, result.Year);
        Assert.Equal(2480m, result.MonthBalance);
        Assert.Equal(6200m, result.Income);
        Assert.Equal(1450m, result.Expenses);
        Assert.Equal(30m, result.SpentToday);
        Assert.Equal(70m, result.AvailableToday);
        Assert.Equal("BRL", result.Currency);
        Assert.Null(result.Goal);
        _summary.Verify(s => s.GetMonthlySummary(_userId, 10, 2026), Times.Once);
    }

    [Fact]
    public async Task GetSummary_IncludesGoalWhenPresent()
    {
        string? storedHash = null;
        _keyRepo.Setup(r => r.UpsertAsync(It.IsAny<WidgetKey>()))
            .Callback<WidgetKey>(k => storedHash = k.KeyHash)
            .ReturnsAsync((WidgetKey k) => k);
        var service = CreateService();
        var created = await service.CreateKeyAsync(_userId);
        _keyRepo.Setup(r => r.GetByHashAsync(storedHash!))
            .ReturnsAsync(new WidgetKey { Id = Guid.NewGuid(), UserId = _userId, KeyHash = storedHash! });
        _goals.Setup(g => g.GetAsync(_userId)).ReturnsAsync(new GoalResponse
        {
            Name = "Reserva",
            TargetAmount = 100_000m,
            SavedAmount = 38_000m,
            ProgressPercent = 38m
        });

        var result = await service.GetSummaryAsync(created.Key, null);

        Assert.NotNull(result!.Goal);
        Assert.Equal("Reserva", result.Goal!.Name);
        Assert.Equal(38m, result.Goal.ProgressPercent);
    }

    [Fact]
    public async Task RevokeKey_DeletesByUser()
    {
        await CreateService().RevokeKeyAsync(_userId);

        _keyRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
    }

    [Fact]
    public async Task KeyStatus_ReflectsWhetherKeyExists()
    {
        var service = CreateService();
        Assert.False((await service.GetKeyStatusAsync(_userId)).Enabled);

        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _keyRepo.Setup(r => r.GetByUserAsync(_userId))
            .ReturnsAsync(new WidgetKey { UserId = _userId, CreatedAt = createdAt });

        var status = await service.GetKeyStatusAsync(_userId);

        Assert.True(status.Enabled);
        Assert.Equal(createdAt, status.CreatedAt);
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
