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
using TagEntity = Zeno.Domain.Tag.Tag;

namespace Zeno.Tests;

public class CaptureServiceTests
{
    private readonly Mock<ICaptureKeyRepository> _keyRepo = new();
    private readonly Mock<ICaptureRuleRepository> _ruleRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IEntryService> _entryService = new();
    private readonly Mock<ICaptureCardRepository> _cardRepo = new();
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly List<CaptureCard> _cards = new();
    private CreateEntryRequest? _lastCreated;
    private readonly FakeClock _clock = new();
    private readonly Guid _userId = Guid.NewGuid();

    private CaptureKey? _stored;
    private readonly List<CaptureRule> _rules = new();
    private readonly List<TagEntity> _tags = new();

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

        _ruleRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => (IReadOnlyList<CaptureRule>)_rules.ToList());
        _ruleRepo.Setup(r => r.CreateAsync(It.IsAny<CaptureRule>()))
            .Callback<CaptureRule>(r => _rules.Add(r))
            .ReturnsAsync((CaptureRule r) => r);
        _ruleRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Guid user, Guid id) => _rules.RemoveAll(x => x.Id == id && x.UserId == user) > 0);
        _tagRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => (IEnumerable<TagEntity>)_tags.ToList());
        _tagRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _tags.FirstOrDefault(t => t.Id == id));

        // In-memory card store.
        _cardRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid u) => (IReadOnlyList<CaptureCard>)_cards.Where(c => c.UserId == u).ToList());
        _cardRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Guid u, Guid id) => _cards.FirstOrDefault(c => c.UserId == u && c.Id == id));
        _cardRepo.Setup(r => r.GetByNameKeyAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Guid u, string k) => _cards.FirstOrDefault(c => c.UserId == u && c.NameKey == k));
        _cardRepo.Setup(r => r.UpsertAsync(It.IsAny<CaptureCard>()))
            .Callback<CaptureCard>(c => { _cards.RemoveAll(x => x.Id == c.Id); _cards.Add(c); })
            .Returns(Task.CompletedTask);

        _entryService.Setup(s => s.CreateEntry(It.IsAny<Guid>(), It.IsAny<CreateEntryRequest>()))
            .Callback((Guid _, CreateEntryRequest r) => _lastCreated = r)
            .ReturnsAsync((Guid _, CreateEntryRequest r) => new EntryEntity
            {
                Id = Guid.NewGuid(),
                Title = r.Title,
                Value = r.Value,
                Kind = r.Kind,
                Date = r.Date,
                Description = r.Description ?? string.Empty,
                TagId = r.TagId
            });
    }

    private CaptureService CreateService() =>
        new(_keyRepo.Object, _ruleRepo.Object, _tagRepo.Object, _entryService.Object, _cardRepo.Object, _entryRepo.Object, _clock);

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

    private TagEntity AddTag(string name, Guid? ownerId = null)
    {
        var tag = new TagEntity { Id = Guid.NewGuid(), UserId = ownerId ?? _userId, Name = name };
        _tags.Add(tag);
        return tag;
    }

    [Fact]
    public async Task Capture_WithoutCardOrCategory_DescriptionIsDefault_AndNoTag()
    {
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest { Title = "Padaria", Amount = Json("10") }, null);

        Assert.Equal("Capturado automaticamente", result!.Description);
        Assert.Null(result.TagId);
    }

    [Fact]
    public async Task Capture_CardAndCategory_AreAddedToDescription()
    {
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest
        {
            Title = "Padaria",
            Amount = Json("10"),
            Card = "  Nubank Mastercard ",
            Category = "Alimentação"
        }, null);

        Assert.Equal("Capturado automaticamente · Cartão: Nubank Mastercard · Categoria: Alimentação", result!.Description);
    }

    [Fact]
    public async Task Capture_CategoryMatchingTagName_IgnoringCaseAndAccents_SetsTag()
    {
        var food = AddTag("alimentacao");
        AddTag("Transporte");
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest
        {
            Title = "Padaria", Amount = Json("10"), Category = "Alimentação"
        }, null);

        Assert.Equal(food.Id, result!.TagId);
    }

    [Fact]
    public async Task Capture_TagNameContainedInCategory_SetsTag()
    {
        var market = AddTag("Mercado");
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest
        {
            Title = "Loja", Amount = Json("10"), Category = "Mercado e Supermercados"
        }, null);

        Assert.Equal(market.Id, result!.TagId);
    }

    [Fact]
    public async Task Capture_RuleOnMerchant_BeatsCategoryTagName()
    {
        var food = AddTag("Alimentação");
        var delivery = AddTag("Delivery");
        _rules.Add(new CaptureRule { Id = Guid.NewGuid(), UserId = _userId, Match = "IFOOD", TagId = delivery.Id });
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest
        {
            Title = "iFood *Restaurante", Amount = Json("40"), Category = "Alimentação"
        }, null);

        Assert.Equal(delivery.Id, result!.TagId);
        Assert.NotEqual(food.Id, result.TagId);
    }

    [Fact]
    public async Task Capture_RuleOnCategory_SetsTag()
    {
        var health = AddTag("Saúde");
        _rules.Add(new CaptureRule { Id = Guid.NewGuid(), UserId = _userId, Match = "farmacia", TagId = health.Id });
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest
        {
            Title = "Loja 123", Amount = Json("25"), Category = "Farmácias"
        }, null);

        Assert.Equal(health.Id, result!.TagId);
    }

    [Fact]
    public async Task Capture_NoRuleOrTagMatch_LeavesEntryUntagged()
    {
        AddTag("Transporte");
        var (service, key) = await ServiceWithKeyAsync();

        var result = await service.CaptureAsync(key, new CaptureEntryRequest
        {
            Title = "Padaria", Amount = Json("10"), Category = "Alimentação"
        }, null);

        Assert.Null(result!.TagId);
    }

    [Fact]
    public async Task AddRule_ValidTag_IsSaved_TrimmedAndListed()
    {
        var tag = AddTag("Alimentação");
        var service = CreateService();

        var created = await service.AddRuleAsync(_userId, new AddCaptureRuleRequest { Match = "  ifood ", TagId = tag.Id });
        var all = await service.GetRulesAsync(_userId);

        Assert.Equal("ifood", created.Match);
        Assert.Single(all);
        Assert.Equal(tag.Id, all[0].TagId);
    }

    [Fact]
    public async Task AddRule_EmptyMatch_TooLong_OrForeignTag_Throw()
    {
        var mine = AddTag("Minha");
        var foreign = AddTag("De outro", Guid.NewGuid());
        var service = CreateService();

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.AddRuleAsync(_userId, new AddCaptureRuleRequest { Match = "  ", TagId = mine.Id }));
        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.AddRuleAsync(_userId, new AddCaptureRuleRequest { Match = new string('a', 61), TagId = mine.Id }));
        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.AddRuleAsync(_userId, new AddCaptureRuleRequest { Match = "x", TagId = foreign.Id }));
        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.AddRuleAsync(_userId, new AddCaptureRuleRequest { Match = "x", TagId = Guid.NewGuid() }));
        _ruleRepo.Verify(r => r.CreateAsync(It.IsAny<CaptureRule>()), Times.Never);
    }

    [Fact]
    public async Task AddRule_Over50Rules_Throws()
    {
        var tag = AddTag("Tag");
        for (var i = 0; i < 50; i++)
            _rules.Add(new CaptureRule { Id = Guid.NewGuid(), UserId = _userId, Match = $"r{i}", TagId = tag.Id });

        await Assert.ThrowsAsync<AppValidationException>(() =>
            CreateService().AddRuleAsync(_userId, new AddCaptureRuleRequest { Match = "novo", TagId = tag.Id }));
    }

    [Fact]
    public async Task DeleteRule_OnlyDeletesOwnRules()
    {
        var tag = AddTag("Tag");
        var otherUser = Guid.NewGuid();
        var mine = new CaptureRule { Id = Guid.NewGuid(), UserId = _userId, Match = "a", TagId = tag.Id };
        var theirs = new CaptureRule { Id = Guid.NewGuid(), UserId = otherUser, Match = "b", TagId = tag.Id };
        _rules.AddRange(new[] { mine, theirs });
        var service = CreateService();

        await service.DeleteRuleAsync(_userId, mine.Id);
        await Assert.ThrowsAsync<AppValidationException>(() => service.DeleteRuleAsync(_userId, theirs.Id));

        Assert.DoesNotContain(_rules, r => r.Id == mine.Id);
        Assert.Contains(_rules, r => r.Id == theirs.Id);
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

    private Task<Zeno.Application.Responses.Capture.CaptureEntryResponse?> Buy(CaptureService service, string key, string card, string title = "Padaria", string amount = "24.50", string? kind = null)
    {
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5); // past the duplicate window
        return service.CaptureAsync(key, new CaptureEntryRequest { Title = title, Amount = Json(amount), Card = card, Kind = kind }, "America/Sao_Paulo");
    }

    [Fact]
    public async Task Capture_NewCard_IsRemembered_Unclassified_AndEntryIsDiario()
    {
        var (service, key) = await ServiceWithKeyAsync();

        var result = await Buy(service, key, "Nubank");

        var card = Assert.Single(_cards);
        Assert.Equal("Nubank", card.Name);
        Assert.Null(card.Kind);
        Assert.Equal(1, card.PurchaseCount);
        Assert.Equal(EntryKind.Diario, result!.Kind);
        Assert.Equal(card.Id, _lastCreated!.CaptureCardId);
        Assert.False(result.CardClassified);
    }

    [Fact]
    public async Task Capture_SameCardDifferentCase_IsOneCard()
    {
        var (service, key) = await ServiceWithKeyAsync();

        await Buy(service, key, "Nubank");
        await Buy(service, key, "NUBANK", title: "Mercado");

        var card = Assert.Single(_cards);
        Assert.Equal(2, card.PurchaseCount);
    }

    [Fact]
    public async Task Capture_CreditCard_BecomesGastoComCartao()
    {
        var (service, key) = await ServiceWithKeyAsync();
        await Buy(service, key, "Nubank");
        await service.ClassifyCardAsync(_userId, _cards[0].Id, new ClassifyCaptureCardRequest { Type = "credit" });

        var result = await Buy(service, key, "Nubank", title: "Mercado");

        Assert.Equal(EntryKind.Cartao, result!.Kind);
        Assert.True(result.CardClassified);
    }

    [Fact]
    public async Task Capture_ExplicitKindFromShortcut_WinsOverTheCard()
    {
        var (service, key) = await ServiceWithKeyAsync();
        await Buy(service, key, "Nubank");
        await service.ClassifyCardAsync(_userId, _cards[0].Id, new ClassifyCaptureCardRequest { Type = "credit" });

        var result = await Buy(service, key, "Nubank", title: "Farmácia", kind: "diario");

        Assert.Equal(EntryKind.Diario, result!.Kind);
    }

    [Fact]
    public async Task Classify_ApplyToPast_ReclassifiesThatCardsEntries()
    {
        var (service, key) = await ServiceWithKeyAsync();
        await Buy(service, key, "Itaú");
        var cardId = _cards[0].Id;
        _entryRepo.Setup(r => r.SetKindByCaptureCardAsync(_userId, cardId, EntryKind.Cartao)).ReturnsAsync(3);

        var result = await service.ClassifyCardAsync(_userId, cardId, new ClassifyCaptureCardRequest { Type = "credit", ApplyToPast = true });

        Assert.Equal("credit", result.Card.Type);
        Assert.Equal(3, result.UpdatedEntries);
    }

    [Fact]
    public async Task Classify_WithoutApplyToPast_LeavesEarlierEntries()
    {
        var (service, key) = await ServiceWithKeyAsync();
        await Buy(service, key, "Itaú");

        await service.ClassifyCardAsync(_userId, _cards[0].Id, new ClassifyCaptureCardRequest { Type = "debit" });

        _entryRepo.Verify(r => r.SetKindByCaptureCardAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<EntryKind>()), Times.Never);
    }

    [Fact]
    public async Task Classify_AnotherUsersCard_OrBadType_IsRejected()
    {
        var (service, key) = await ServiceWithKeyAsync();
        await Buy(service, key, "Itaú");

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.ClassifyCardAsync(Guid.NewGuid(), _cards[0].Id, new ClassifyCaptureCardRequest { Type = "credit" }));
        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.ClassifyCardAsync(_userId, _cards[0].Id, new ClassifyCaptureCardRequest { Type = "prepaid" }));
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
