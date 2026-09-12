using System.Globalization;
using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Notifications;
using Zeno.Application.Requests.Notifications;
using Zeno.Application.Responses;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;

namespace Zeno.Application.Services;

public class NotificationService : INotificationService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IValidator<RegisterDeviceRequest> _registerValidator;
    private readonly IValidator<UpdateNotificationPreferenceRequest> _preferenceValidator;
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IPushNotificationSender _sender;
    private readonly IClock _clock;

    public NotificationService(
        IValidator<RegisterDeviceRequest> registerValidator,
        IValidator<UpdateNotificationPreferenceRequest> preferenceValidator,
        IDeviceTokenRepository deviceTokenRepository,
        INotificationPreferenceRepository preferenceRepository,
        IWalletRepository walletRepository,
        IEntryRepository entryRepository,
        IPushNotificationSender sender,
        IClock clock)
    {
        _registerValidator = registerValidator;
        _preferenceValidator = preferenceValidator;
        _deviceTokenRepository = deviceTokenRepository;
        _preferenceRepository = preferenceRepository;
        _walletRepository = walletRepository;
        _entryRepository = entryRepository;
        _sender = sender;
        _clock = clock;
    }

    public async Task<DeviceTokenResponse> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var now = _clock.UtcNow;

        var deviceToken = new DeviceToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = request.Token.Trim(),
            Platform = request.Platform,
            IsActive = true,
            CreatedAt = now,
            LastSeenAt = now
        };

        var saved = await _deviceTokenRepository.UpsertAsync(deviceToken);

        // Registrar um aparelho so faz sentido se o usuario ja tiver uma preferencia;
        // criamos uma desligada para que o app consiga ler o estado logo apos o primeiro login.
        var preference = await _preferenceRepository.GetByUserAsync(userId);
        if (preference is null)
            await _preferenceRepository.UpsertAsync(NewDefaultPreference(userId, now));

        return new DeviceTokenResponse
        {
            Id = saved.Id,
            Platform = saved.Platform,
            CreatedAt = saved.CreatedAt,
            LastSeenAt = saved.LastSeenAt
        };
    }

    public async Task UnregisterDeviceAsync(Guid userId, string token)
    {
        await _deviceTokenRepository.DeleteByUserAndTokenAsync(userId, token);
    }

    public async Task<NotificationPreferenceResponse> GetPreferenceAsync(Guid userId)
    {
        var preference = await _preferenceRepository.GetByUserAsync(userId)
            ?? NewDefaultPreference(userId, _clock.UtcNow);

        return await BuildPreferenceResponseAsync(preference);
    }

    public async Task<NotificationPreferenceResponse> UpdatePreferenceAsync(Guid userId, UpdateNotificationPreferenceRequest request)
    {
        var validation = await _preferenceValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var now = _clock.UtcNow;
        var existing = await _preferenceRepository.GetByUserAsync(userId) ?? NewDefaultPreference(userId, now);

        existing.DailyEnabled = request.DailyEnabled;
        existing.SendHour = request.SendHour;
        existing.TimeZoneId = request.TimeZoneId;
        existing.UpdatedAt = now;

        var saved = await _preferenceRepository.UpsertAsync(existing);

        return await BuildPreferenceResponseAsync(saved);
    }

    public async Task<SendTestNotificationResponse> SendTestAsync(Guid userId)
    {
        var tokens = (await _deviceTokenRepository.GetActiveByUserAsync(userId))
            .Select(d => d.Token)
            .ToList();

        if (!_sender.IsConfigured)
        {
            return new SendTestNotificationResponse
            {
                PushConfigured = false,
                DevicesTargeted = tokens.Count,
                Message = "O servidor nao tem credencial de push configurada, entao nenhuma notificacao sai. Configure a secao Push:Firebase."
            };
        }

        if (tokens.Count == 0)
        {
            return new SendTestNotificationResponse
            {
                PushConfigured = true,
                DevicesTargeted = 0,
                Message = "Nenhum aparelho registrado para este usuario. O app precisa chamar POST /api/notifications/devices com o token do dispositivo."
            };
        }

        var message = new PushMessage
        {
            Title = "Zeno",
            Body = "Notificacao de teste. Se voce esta lendo isso, o push esta funcionando.",
            Data = new Dictionary<string, string> { ["type"] = "test" }
        };

        var result = await _sender.SendAsync(tokens, message);
        var removed = await PruneInvalidTokensAsync(result);

        return new SendTestNotificationResponse
        {
            PushConfigured = true,
            DevicesTargeted = tokens.Count,
            SuccessCount = result.SuccessCount,
            InvalidTokensRemoved = removed,
            Message = result.SuccessCount > 0
                ? "Notificacao enviada."
                : "Nenhum envio foi aceito pelo provedor. Verifique se o token do aparelho ainda e valido."
        };
    }

    public async Task<int> SendDueDailyDigestsAsync(CancellationToken cancellationToken = default)
    {
        if (!_sender.IsConfigured)
            return 0;

        var nowUtc = _clock.UtcNow;
        var preferences = await _preferenceRepository.GetAllEnabledAsync();
        var sent = 0;

        foreach (var preference in preferences)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                if (await TrySendDigestAsync(preference, nowUtc, cancellationToken))
                    sent++;
            }
            catch (Exception ex)
            {
                // Uma falha isolada nao pode derrubar a varredura dos demais usuarios.
                Console.WriteLine($"[NotificationService] Falha ao enviar resumo do usuario {preference.UserId}: {ex.Message}");
            }
        }

        return sent;
    }

    private async Task<bool> TrySendDigestAsync(NotificationPreference preference, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var timeZone = ResolveTimeZone(preference.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var localDate = DateOnly.FromDateTime(localNow);

        if (preference.LastSentOn == localDate)
            return false;

        // ">=" e nao "==": se a varredura atrasar (restart, fila), o resumo ainda sai no mesmo dia.
        if (localNow.Hour < preference.SendHour)
            return false;

        var tokens = (await _deviceTokenRepository.GetActiveByUserAsync(preference.UserId))
            .Select(d => d.Token)
            .ToList();

        if (tokens.Count == 0)
            return false;

        var message = await BuildDailyMessageAsync(preference.UserId, localNow);
        if (message is null)
            return false;

        var result = await _sender.SendAsync(tokens, message, cancellationToken);
        await PruneInvalidTokensAsync(result);

        if (result.SuccessCount == 0)
            return false;

        await _preferenceRepository.MarkSentAsync(preference.UserId, localDate);
        return true;
    }

    private async Task<PushMessage?> BuildDailyMessageAsync(Guid userId, DateTime localNow)
    {
        var wallets = (await _walletRepository.GetAllByUserAsync(userId)).ToList();
        if (wallets.Count == 0)
            return null;

        var daysInMonth = DateTime.DaysInMonth(localNow.Year, localNow.Month);
        var remainingDays = Math.Max(daysInMonth - localNow.Day + 1, 1);

        decimal budgetedRemaining = 0;
        var hasBudget = false;

        foreach (var wallet in wallets.Where(w => w.DailyBudget.HasValue && w.Id.HasValue))
        {
            hasBudget = true;

            var spent = await _entryRepository.GetSumByKindAsync(wallet.Id!.Value, EntryKind.Diario, localNow.Month, localNow.Year);
            budgetedRemaining += wallet.DailyBudget!.Value * daysInMonth - spent;
        }

        if (hasBudget)
        {
            if (budgetedRemaining < 0)
            {
                return new PushMessage
                {
                    Title = "Orcamento estourado",
                    Body = $"Voce ja passou {Money(-budgetedRemaining)} do orcamento do mes.",
                    Data = new Dictionary<string, string> { ["type"] = "daily-digest", ["status"] = "over-budget" }
                };
            }

            var perDay = budgetedRemaining / remainingDays;

            return new PushMessage
            {
                Title = "Seu limite de hoje",
                Body = $"Voce pode gastar {Money(perDay)} hoje.",
                Data = new Dictionary<string, string> { ["type"] = "daily-digest", ["status"] = "on-track" }
            };
        }

        var total = wallets.Sum(w => w.Balance);

        return new PushMessage
        {
            Title = "Resumo do dia",
            Body = $"Seu saldo total e {Money(total)}. Defina um orcamento diario para receber quanto pode gastar.",
            Data = new Dictionary<string, string> { ["type"] = "daily-digest", ["status"] = "no-budget" }
        };
    }

    private async Task<int> PruneInvalidTokensAsync(PushSendResult result)
    {
        var removed = 0;

        foreach (var token in result.InvalidTokens)
        {
            await _deviceTokenRepository.DeactivateAsync(token);
            removed++;
        }

        return removed;
    }

    private async Task<NotificationPreferenceResponse> BuildPreferenceResponseAsync(NotificationPreference preference)
    {
        var activeDevices = (await _deviceTokenRepository.GetActiveByUserAsync(preference.UserId)).Count();

        return new NotificationPreferenceResponse
        {
            DailyEnabled = preference.DailyEnabled,
            SendHour = preference.SendHour,
            TimeZoneId = preference.TimeZoneId,
            LastSentOn = preference.LastSentOn,
            ActiveDevices = activeDevices,
            PushConfigured = _sender.IsConfigured
        };
    }

    private static NotificationPreference NewDefaultPreference(Guid userId, DateTime now) => new()
    {
        UserId = userId,
        DailyEnabled = false,
        SendHour = NotificationPreference.DefaultSendHour,
        TimeZoneId = NotificationPreference.DefaultTimeZoneId,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz)
            ? tz
            : TimeZoneInfo.Utc;
    }

    private static string Money(decimal value) => value.ToString("C", PtBr);
}
