using System.Text.Json;
using Moq;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Capture;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Services;
using Zeno.Domain.Capture;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Tests;

public class CaptureServiceTests
{
    private readonly Mock<ICaptureKeyRepository> _keyRepo = new();
    private readonly Mock<IEntryService> _entryService = new();
    private readonly FakeClock _clock = new();
    private readonly Guid _userId = Guid.NewGuid();

    private CaptureKey? _stored;

    public CaptureServiceTests()
    {
        // 02:00 UTC on Oct 4 is still Oct 3 (23:00) in America/Sao_Paulo.
        _clock.UtcNow = new DateTime(2026, 10, 4, 2, 0, 0, DateTimeKind.Utc);

        _keyRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>())).ReturnsAsync(() => _stored);
        _keyRepo.Setup(r => r.UpsertAsync(It.IsAny<CaptureKey>()))
            .Callback<CaptureKey>(k => _stored = k)
            .ReturnsAsync((CaptureKey k) => k);
        _keyRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>()))
            .ReturnsAsync((string hash) => _stored is not null && _stored.KeyHash == hash ? _stored : null);

        _entryService.Setup(s => s.CreateEntry(It.IsAny<Guid>(), It.IsAny<CreateEntryRequest>()))
            .ReturnsAsync((Guid _, CreateEntryRequest r) => new EntryEntity
            {
                Id = Guid.NewGuid(),
                Title = r.Title,
                Value = r.Value,
                Kind = r.Kind,
                Date = r.Date
            });
    }

    private CaptureService CreateService() => new(_keyRepo.Object, _entryService.Object, _clock);

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private async Task<(CaptureService Service, string Key)> ServiceWithKeyAsync()
    {
        var service = CreateService();
        var created = await service.CreateKeyAsync(_userId);
        return (service, created.Key);
    }

    [Fact]
    public async Task CreateKey_ReturnsPrefixedKey_AndStoresOnlyItsHash()
    {
        var created = await CreateService().CreateKeyAsync(_userId);

        Assert.StartsWith("zc_", created.Key);
        Assert.NotNull(_stored);
        Assert.NotEqual(created.Key, _stored!.KeyHash);
    }

    [Fact]
    public async Task Capture_WithMissingOrUnknownKey_ReturnsNull_AndCreatesNothing()
    {
        var service = CreateService();
        var request = new CaptureEntryRequest { Title = "Mercado", Amount = Json("25.9") };

        Assert.Null(await service.CaptureAsync(null, request, null));
        Assert.Null(await service.CaptureAsync("zc_unknown", request, null));
        _entryService.Verify(s => s.CreateEntry(It.IsAny<Guid>(), It.IsAny<CreateEntryRequest>()), Times.Never);
    }

    [Fact]
    public async Task Capture_NumericAmount_CreatesDailyEntryOnLocalDay()
    {
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest { Title = "Padaria", Amount = Json("12.5") }, null);

        Assert.True(result!.Created);
        Assert.Equal(EntryKind.Diario, result.Kind);
        Assert.Equal(12.5m, result.Value);
        _entryService.Verify(s => s.CreateEntry(_userId, It.Is<CreateEntryRequest>(r =>
            r.Title == "Padaria" && r.Value == 12.5m && r.Kind == EntryKind.Diario && r.Date == new DateTime(2026, 10, 3))), Times.Once);
    }

    [Fact]
    public async Task Capture_TextAmountWithCurrency_IsParsed_AndNegativeUsesAbsoluteValue()
    {
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest { Title = "Loja", Amount = Json("\"-R$ 1.234,56\"") }, null);

        Assert.Equal(1234.56m, result!.Value);
    }

    [Fact]
    public async Task Capture_EmptyTitle_DefaultsToApplePay_AndLongTitleIsTruncated()
    {
        var (service, key) = await ServiceWithKeyAsync();

        var empty = await service.CaptureAsync(key, new CaptureEntryRequest { Title = "  ", Amount = Json("5") }, null);
        var longTitle = await service.CaptureAsync(key, new CaptureEntryRequest { Title = new string('x', 300), Amount = Json("6") }, null);

        Assert.Equal("Apple Pay", empty!.Title);
        Assert.Equal(100, longTitle!.Title.Length);
    }

    [Theory]
    [InlineData("entrada", EntryKind.Entrada)]
    [InlineData("Saída", EntryKind.Saida)]
    [InlineData("cartao", EntryKind.Cartao)]
    [InlineData("economia", EntryKind.Economia)]
    [InlineData(null, EntryKind.Diario)]
    public async Task Capture_Kind_IsMapped(string? kind, EntryKind expected)
    {
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest { Title = "x", Amount = Json("10"), Kind = kind }, null);

        Assert.Equal(expected, result!.Kind);
    }

    [Fact]
    public async Task Capture_UnknownKind_Throws()
    {
        var (service, key) = await ServiceWithKeyAsync();

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.CaptureAsync(key, new CaptureEntryRequest { Title = "x", Amount = Json("10"), Kind = "pix" }, null));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("\"abc\"")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task Capture_InvalidAmount_Throws_AndCreatesNothing(string amount)
    {
        var (service, key) = await ServiceWithKeyAsync();

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.CaptureAsync(key, new CaptureEntryRequest { Title = "x", Amount = Json(amount) }, null));
        _entryService.Verify(s => s.CreateEntry(It.IsAny<Guid>(), It.IsAny<CreateEntryRequest>()), Times.Never);
    }

    [Fact]
    public async Task Capture_MissingAmount_Throws()
    {
        var (service, key) = await ServiceWithKeyAsync();

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.CaptureAsync(key, new CaptureEntryRequest { Title = "x" }, null));
    }

    [Fact]
    public async Task Capture_SameEntryTwiceWithinWindow_IsIgnoredAsDuplicate()
    {
        var (service, key) = await ServiceWithKeyAsync();
        var request = new CaptureEntryRequest { Title = "Café", Amount = Json("8.5") };

        var first = await service.CaptureAsync(key, request, null);
        _clock.UtcNow = _clock.UtcNow.AddSeconds(30);
        var second = await service.CaptureAsync(key, request, null);

        Assert.True(first!.Created);
        Assert.False(second!.Created);
        Assert.True(second.Duplicate);
        _entryService.Verify(s => s.CreateEntry(It.IsAny<Guid>(), It.IsAny<CreateEntryRequest>()), Times.Once);
    }

    [Fact]
    public async Task Capture_SameEntryAfterWindow_IsCreatedAgain()
    {
        var (service, key) = await ServiceWithKeyAsync();
        var request = new CaptureEntryRequest { Title = "Café", Amount = Json("8.5") };

        await service.CaptureAsync(key, request, null);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        var again = await service.CaptureAsync(key, request, null);

        Assert.True(again!.Created);
        _entryService.Verify(s => s.CreateEntry(It.IsAny<Guid>(), It.IsAny<CreateEntryRequest>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Capture_DifferentEntryRightAfter_IsCreated()
    {
        var (service, key) = await ServiceWithKeyAsync();

        await service.CaptureAsync(key, new CaptureEntryRequest { Title = "Café", Amount = Json("8.5") }, null);
        var other = await service.CaptureAsync(key, new CaptureEntryRequest { Title = "Café", Amount = Json("9") }, null);

        Assert.True(other!.Created);
    }

    [Fact]
    public async Task Revoke_DeletesByUser_AndStatusReflectsKey()
    {
        var service = CreateService();
        Assert.False((await service.GetKeyStatusAsync(_userId)).Enabled);

        await service.CreateKeyAsync(_userId);
        Assert.True((await service.GetKeyStatusAsync(_userId)).Enabled);

        await service.RevokeKeyAsync(_userId);
        _keyRepo.Verify(r => r.DeleteByUserAsync(_userId), Times.Once);
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
