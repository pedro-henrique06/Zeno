using FluentValidation;
using Moq;
using Zeno.Application.Interfaces;
using Zeno.Application.Notifications;
using Zeno.Application.Requests.Notifications;
using Zeno.Application.Services;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using UserEntity = Zeno.Domain.User.User;
using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Tests;

public class NotificationServiceTests
{
    private const string SaoPaulo = "America/Sao_Paulo";

    private readonly Mock<IValidator<RegisterDeviceRequest>> _registerValidator = new();
    private readonly Mock<IValidator<UpdateNotificationPreferenceRequest>> _preferenceValidator = new();
    private readonly Mock<IDeviceTokenRepository> _deviceRepo = new();
    private readonly Mock<INotificationPreferenceRepository> _preferenceRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly Mock<IPushNotificationSender> _sender = new();
    private readonly FakeClock _clock = new();

    private readonly Guid _userId = Guid.NewGuid();

    public NotificationServiceTests()
    {
        _registerValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterDeviceRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _preferenceValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateNotificationPreferenceRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _sender.SetupGet(s => s.IsConfigured).Returns(true);
        _sender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<string> tokens, PushMessage _, CancellationToken _) =>
                new PushSendResult { SuccessCount = tokens.Count });

        _deviceRepo.Setup(r => r.GetActiveByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<DeviceToken>());
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((UserEntity?)null);
        _entryRepo.Setup(r => r.GetByUserInRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Array.Empty<EntryEntity>());
        _entryRepo.Setup(r => r.GetSignedBalanceBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .ReturnsAsync(0m);
        _preferenceRepo.Setup(r => r.GetAllEnabledAsync())
            .ReturnsAsync(Array.Empty<NotificationPreference>());
    }

    private NotificationService CreateService() => new(
        _registerValidator.Object,
        _preferenceValidator.Object,
        _deviceRepo.Object,
        _preferenceRepo.Object,
        _userRepo.Object,
        _entryRepo.Object,
        _sender.Object,
        _clock);

    private void GivenEnabledPreference(int sendHour = 9, DateOnly? lastSentOn = null, string timeZoneId = SaoPaulo)
    {
        _preferenceRepo.Setup(r => r.GetAllEnabledAsync()).ReturnsAsync(new[]
        {
            new NotificationPreference
            {
                UserId = _userId,
                DailyEnabled = true,
                SendHour = sendHour,
                TimeZoneId = timeZoneId,
                LastSentOn = lastSentOn
            }
        });
    }

    private void GivenDevices(params string[] tokens)
    {
        _deviceRepo.Setup(r => r.GetActiveByUserAsync(_userId)).ReturnsAsync(
            tokens.Select(t => new DeviceToken { Id = Guid.NewGuid(), UserId = _userId, Token = t }).ToArray());
    }

    private void GivenUserWithBudget(decimal dailyBudget, decimal spentThisMonth)
    {
        _userRepo.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(
            new UserEntity { Id = _userId, DailyBudget = dailyBudget });
        _entryRepo.Setup(r => r.GetByUserInRangeAsync(_userId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new[]
            {
                new EntryEntity { UserId = _userId, Kind = EntryKind.Diario, Value = spentThisMonth }
            });
    }

    [Fact]
    public async Task SendDueDailyDigests_SendsWhenLocalHourReached()
    {
        // 12:00 UTC = 09:00 em Sao Paulo (UTC-3).
        _clock.UtcNow = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");
        GivenUserWithBudget(dailyBudget: 50m, spentThisMonth: 1000m);

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(1, sent);
        _sender.Verify(s => s.SendAsync(
            It.Is<IReadOnlyCollection<string>>(t => t.Contains("token-a")),
            It.IsAny<PushMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _preferenceRepo.Verify(r => r.MarkSentAsync(_userId, new DateOnly(2026, 8, 24)), Times.Once);
    }

    [Fact]
    public async Task SendDueDailyDigests_DoesNotSendBeforeConfiguredHour()
    {
        // 11:00 UTC = 08:00 local, antes das 09:00 configuradas.
        _clock.UtcNow = new DateTime(2026, 8, 24, 11, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
        _sender.Verify(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendDueDailyDigests_DoesNotRepeatOnSameLocalDay()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9, lastSentOn: new DateOnly(2026, 8, 24));
        GivenDevices("token-a");

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
        _sender.Verify(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendDueDailyDigests_StillSendsLaterInTheDayWhenSweepWasMissed()
    {
        // Passou muito da hora (20:00 local): o resumo do dia ainda deve sair.
        _clock.UtcNow = new DateTime(2026, 8, 24, 23, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9, lastSentOn: new DateOnly(2026, 8, 23));
        GivenDevices("token-a");
        GivenUserWithBudget(dailyBudget: 50m, spentThisMonth: 100m);

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task SendDueDailyDigests_UsesUserTimeZoneNotServerUtc()
    {
        // Com sendHour 22 em Tokyo (UTC+9), 12:00 UTC = 21:00 local -> ainda nao deve enviar.
        _clock.UtcNow = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 22, timeZoneId: "Asia/Tokyo");
        GivenDevices("token-a");

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
    }

    [Fact]
    public async Task SendDueDailyDigests_SkipsUserWithoutRegisteredDevice()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        // Nenhum aparelho registrado.

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
        _preferenceRepo.Verify(r => r.MarkSentAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task SendDueDailyDigests_SkipsUserNotFound()
    {
        // Sem usuario nao ha o que resumir; nao marcamos como enviado.
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");
        // _userRepo default retorna null

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
        _preferenceRepo.Verify(r => r.MarkSentAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task DailyDigest_FallsBackToBalanceWhenNoBudgetConfigured()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");
        _userRepo.Setup(r => r.GetByIdAsync(_userId))
            .ReturnsAsync(new UserEntity { Id = _userId, DailyBudget = null });
        _entryRepo.Setup(r => r.GetSignedBalanceBeforeAsync(_userId, It.IsAny<DateTime>()))
            .ReturnsAsync(250.5m);

        PushMessage? captured = null;
        _sender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyCollection<string> _, PushMessage m, CancellationToken _) => captured = m)
            .ReturnsAsync(new PushSendResult { SuccessCount = 1 });

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(1, sent);
        Assert.NotNull(captured);
        Assert.Equal("no-budget", captured!.Data["status"]);
        Assert.Contains("250,50", captured.Body);
    }

    [Fact]
    public async Task SendDueDailyDigests_DoesNotMarkSentWhenProviderAcceptedNothing()
    {
        // Se nada foi aceito, o dia continua "em aberto" para a proxima varredura tentar de novo.
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");
        GivenUserWithBudget(dailyBudget: 50m, spentThisMonth: 100m);

        _sender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PushSendResult { SuccessCount = 0 });

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
        _preferenceRepo.Verify(r => r.MarkSentAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task SendDueDailyDigests_DoesNothingWhenPushNotConfigured()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(false);
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(0, sent);
        _preferenceRepo.Verify(r => r.GetAllEnabledAsync(), Times.Never);
    }

    [Fact]
    public async Task SendDueDailyDigests_DeactivatesTokensRejectedByProvider()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("valido", "expirado");
        GivenUserWithBudget(dailyBudget: 50m, spentThisMonth: 100m);

        _sender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PushSendResult { SuccessCount = 1, InvalidTokens = new[] { "expirado" } });

        await CreateService().SendDueDailyDigestsAsync();

        _deviceRepo.Verify(r => r.DeactivateAsync("expirado"), Times.Once);
        _deviceRepo.Verify(r => r.DeactivateAsync("valido"), Times.Never);
    }

    [Fact]
    public async Task SendDueDailyDigests_OneUserFailingDoesNotStopTheOthers()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        var brokenUser = Guid.NewGuid();

        _preferenceRepo.Setup(r => r.GetAllEnabledAsync()).ReturnsAsync(new[]
        {
            new NotificationPreference { UserId = brokenUser, DailyEnabled = true, SendHour = 9, TimeZoneId = SaoPaulo },
            new NotificationPreference { UserId = _userId, DailyEnabled = true, SendHour = 9, TimeZoneId = SaoPaulo }
        });

        _deviceRepo.Setup(r => r.GetActiveByUserAsync(brokenUser)).ThrowsAsync(new InvalidOperationException("boom"));
        GivenDevices("token-a");
        GivenUserWithBudget(dailyBudget: 50m, spentThisMonth: 100m);

        var sent = await CreateService().SendDueDailyDigestsAsync();

        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task DailyDigest_UsesRemainingBudgetSpreadOverRemainingDays()
    {
        // Agosto tem 31 dias. No dia 24 restam 8 dias (24..31).
        // Orcamento do mes = 50 * 31 = 1550; gasto = 1000; sobra 550 / 8 = 68,75.
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");
        GivenUserWithBudget(dailyBudget: 50m, spentThisMonth: 1000m);

        PushMessage? captured = null;
        _sender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyCollection<string> _, PushMessage m, CancellationToken _) => captured = m)
            .ReturnsAsync(new PushSendResult { SuccessCount = 1 });

        await CreateService().SendDueDailyDigestsAsync();

        Assert.NotNull(captured);
        Assert.Contains("68,75", captured!.Body);
        Assert.Equal("on-track", captured.Data["status"]);
    }

    [Fact]
    public async Task DailyDigest_ReportsOverBudget()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        GivenEnabledPreference(sendHour: 9);
        GivenDevices("token-a");
        GivenUserWithBudget(dailyBudget: 10m, spentThisMonth: 1000m);

        PushMessage? captured = null;
        _sender.Setup(s => s.SendAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyCollection<string> _, PushMessage m, CancellationToken _) => captured = m)
            .ReturnsAsync(new PushSendResult { SuccessCount = 1 });

        await CreateService().SendDueDailyDigestsAsync();

        Assert.NotNull(captured);
        Assert.Equal("over-budget", captured!.Data["status"]);
    }

    [Fact]
    public async Task GetPreference_ReportsDiagnosticsWhenNothingIsRegistered()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(false);
        _preferenceRepo.Setup(r => r.GetByUserAsync(_userId)).ReturnsAsync((NotificationPreference?)null);

        var result = await CreateService().GetPreferenceAsync(_userId);

        Assert.False(result.DailyEnabled);
        Assert.Equal(0, result.ActiveDevices);
        Assert.False(result.PushConfigured);
        Assert.Equal(NotificationPreference.DefaultTimeZoneId, result.TimeZoneId);
    }

    [Fact]
    public async Task UpdatePreference_PersistsEnabledFlagAndHour()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        _preferenceRepo.Setup(r => r.GetByUserAsync(_userId)).ReturnsAsync((NotificationPreference?)null);
        _preferenceRepo.Setup(r => r.UpsertAsync(It.IsAny<NotificationPreference>()))
            .ReturnsAsync((NotificationPreference p) => p);

        var result = await CreateService().UpdatePreferenceAsync(_userId, new UpdateNotificationPreferenceRequest
        {
            DailyEnabled = true,
            SendHour = 20,
            TimeZoneId = SaoPaulo
        });

        Assert.True(result.DailyEnabled);
        Assert.Equal(20, result.SendHour);
        _preferenceRepo.Verify(r => r.UpsertAsync(It.Is<NotificationPreference>(
            p => p.UserId == _userId && p.DailyEnabled && p.SendHour == 20)), Times.Once);
    }

    [Fact]
    public async Task SendTest_ExplainsWhenNoDeviceIsRegistered()
    {
        var result = await CreateService().SendTestAsync(_userId);

        Assert.True(result.PushConfigured);
        Assert.Equal(0, result.DevicesTargeted);
        Assert.Equal(0, result.SuccessCount);
        Assert.Contains("Nenhum aparelho registrado", result.Message);
    }

    [Fact]
    public async Task SendTest_ExplainsWhenServerHasNoCredentials()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(false);
        GivenDevices("token-a");

        var result = await CreateService().SendTestAsync(_userId);

        Assert.False(result.PushConfigured);
        Assert.Contains("credencial de push", result.Message);
    }

    [Fact]
    public async Task RegisterDevice_UpsertsTokenAndCreatesDefaultPreference()
    {
        _clock.UtcNow = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        _preferenceRepo.Setup(r => r.GetByUserAsync(_userId)).ReturnsAsync((NotificationPreference?)null);
        _deviceRepo.Setup(r => r.UpsertAsync(It.IsAny<DeviceToken>()))
            .ReturnsAsync((DeviceToken d) => d);

        var result = await CreateService().RegisterDeviceAsync(_userId, new RegisterDeviceRequest
        {
            Token = "  token-com-espaco  ",
            Platform = DevicePlatform.Ios
        });

        Assert.Equal(DevicePlatform.Ios, result.Platform);
        _deviceRepo.Verify(r => r.UpsertAsync(It.Is<DeviceToken>(
            d => d.Token == "token-com-espaco" && d.UserId == _userId && d.IsActive)), Times.Once);
        _preferenceRepo.Verify(r => r.UpsertAsync(It.Is<NotificationPreference>(p => !p.DailyEnabled)), Times.Once);
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
