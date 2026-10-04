using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation.Results;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Capture;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Responses.Capture;
using Zeno.Domain.Capture;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;

namespace Zeno.Application.Services;

public class CaptureService : ICaptureService
{
    private const string KeyPrefix = "zc_";
    private const int MaxTitleLength = 100;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(2);

    private readonly ICaptureKeyRepository _keyRepository;
    private readonly IEntryService _entryService;
    private readonly IClock _clock;

    public CaptureService(
        ICaptureKeyRepository keyRepository,
        IEntryService entryService,
        IClock clock)
    {
        _keyRepository = keyRepository;
        _entryService = entryService;
        _clock = clock;
    }

    public async Task<CaptureKeyStatusResponse> GetKeyStatusAsync(Guid userId)
    {
        var existing = await _keyRepository.GetByUserAsync(userId);
        return new CaptureKeyStatusResponse
        {
            Enabled = existing is not null,
            CreatedAt = existing?.CreatedAt
        };
    }

    public async Task<CaptureKeyResponse> CreateKeyAsync(Guid userId)
    {
        var plainKey = KeyPrefix + ToBase64Url(RandomNumberGenerator.GetBytes(32));
        var existing = await _keyRepository.GetByUserAsync(userId);

        var record = existing ?? new CaptureKey { Id = Guid.NewGuid(), UserId = userId };
        record.KeyHash = Hash(plainKey);
        record.CreatedAt = _clock.UtcNow;
        record.LastFingerprint = null;
        record.LastCaptureAt = null;

        await _keyRepository.UpsertAsync(record);

        return new CaptureKeyResponse { Key = plainKey, CreatedAt = record.CreatedAt };
    }

    public Task RevokeKeyAsync(Guid userId)
    {
        return _keyRepository.DeleteByUserAsync(userId);
    }

    public async Task<CaptureEntryResponse?> CaptureAsync(string? key, CaptureEntryRequest request, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var record = await _keyRepository.GetByHashAsync(Hash(key.Trim()));
        if (record is null)
            return null;

        var value = ParseAmount(request.Amount);
        var kind = ParseKind(request.Kind);
        var title = NormalizeTitle(request.Title);

        var nowUtc = _clock.UtcNow;
        var date = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ResolveTimeZone(timeZoneId)).Date;

        var fingerprint = Fingerprint(title, value, kind, date);
        if (record.LastFingerprint == fingerprint
            && record.LastCaptureAt.HasValue
            && nowUtc - record.LastCaptureAt.Value < DuplicateWindow)
        {
            return new CaptureEntryResponse
            {
                Created = false,
                Duplicate = true,
                Title = title,
                Value = value,
                Kind = kind,
                Date = date
            };
        }

        var entry = await _entryService.CreateEntry(record.UserId, new CreateEntryRequest
        {
            Title = title,
            Value = value,
            Kind = kind,
            Description = "Capturado automaticamente",
            Date = date
        });

        record.LastFingerprint = fingerprint;
        record.LastCaptureAt = nowUtc;
        await _keyRepository.UpsertAsync(record);

        return new CaptureEntryResponse
        {
            Created = true,
            EntryId = entry.Id,
            Title = entry.Title,
            Value = entry.Value,
            Kind = entry.Kind,
            Date = entry.Date
        };
    }

    private static decimal ParseAmount(JsonElement? amount)
    {
        decimal parsed = 0m;
        var ok = false;

        if (amount is { } element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    ok = element.TryGetDecimal(out parsed);
                    break;
                case JsonValueKind.String:
                    ok = AmountParser.TryParse(element.GetString(), out parsed);
                    break;
            }
        }

        parsed = Math.Abs(parsed);
        if (!ok || parsed <= 0m)
            throw Invalid(nameof(CaptureEntryRequest.Amount), "O valor é obrigatório e deve ser maior que zero.");

        return Math.Round(parsed, 2);
    }

    private static EntryKind ParseKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return EntryKind.Diario;

        return kind.Trim().ToLowerInvariant() switch
        {
            "diario" or "diário" or "daily" => EntryKind.Diario,
            "entrada" or "income" => EntryKind.Entrada,
            "saida" or "saída" or "expense" => EntryKind.Saida,
            "economia" or "savings" => EntryKind.Economia,
            "cartao" or "cartão" or "card" => EntryKind.Cartao,
            _ => throw Invalid(nameof(CaptureEntryRequest.Kind), "Tipo inválido. Use diario, entrada, saida, economia ou cartao.")
        };
    }

    private static string NormalizeTitle(string? title)
    {
        var trimmed = string.IsNullOrWhiteSpace(title) ? "Apple Pay" : title.Trim();
        return trimmed.Length > MaxTitleLength ? trimmed[..MaxTitleLength] : trimmed;
    }

    private static AppValidationException Invalid(string property, string message)
    {
        return new AppValidationException(new ValidationResult(new List<ValidationFailure>
        {
            new(property, message)
        }));
    }

    private static string Fingerprint(string title, decimal value, EntryKind kind, DateTime date)
    {
        return Hash($"{title}|{value:0.00}|{(int)kind}|{date:yyyy-MM-dd}");
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? NotificationPreference.DefaultTimeZoneId : timeZoneId;
        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : TimeZoneInfo.Utc;
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
