using System.Globalization;
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
    private const int MaxDetailLength = 100;
    private const int MaxMatchLength = 60;
    private const int MaxRulesPerUser = 50;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(2);

    private readonly ICaptureKeyRepository _keyRepository;
    private readonly ICaptureRuleRepository _ruleRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IEntryService _entryService;
    private readonly IClock _clock;

    public CaptureService(
        ICaptureKeyRepository keyRepository,
        ICaptureRuleRepository ruleRepository,
        ITagRepository tagRepository,
        IEntryService entryService,
        IClock clock)
    {
        _keyRepository = keyRepository;
        _ruleRepository = ruleRepository;
        _tagRepository = tagRepository;
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

    public async Task<IReadOnlyList<CaptureRuleResponse>> GetRulesAsync(Guid userId)
    {
        var rules = await _ruleRepository.GetByUserAsync(userId);
        return rules.Select(ToResponse).ToList();
    }

    public async Task<CaptureRuleResponse> AddRuleAsync(Guid userId, AddCaptureRuleRequest request)
    {
        var match = request.Match?.Trim() ?? string.Empty;
        if (match.Length == 0)
            throw Invalid(nameof(AddCaptureRuleRequest.Match), "Informe o texto que deve ser procurado.");
        if (match.Length > MaxMatchLength)
            throw Invalid(nameof(AddCaptureRuleRequest.Match), $"O texto deve ter no máximo {MaxMatchLength} caracteres.");

        var tag = await _tagRepository.GetByIdAsync(request.TagId);
        if (tag is null || tag.UserId != userId)
            throw Invalid(nameof(AddCaptureRuleRequest.TagId), "Tag não encontrada.");

        var existing = await _ruleRepository.GetByUserAsync(userId);
        if (existing.Count >= MaxRulesPerUser)
            throw Invalid(nameof(AddCaptureRuleRequest.Match), $"Limite de {MaxRulesPerUser} regras atingido.");

        var rule = await _ruleRepository.CreateAsync(new CaptureRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Match = match,
            TagId = tag.Id,
            CreatedAt = _clock.UtcNow
        });

        return ToResponse(rule);
    }

    public async Task DeleteRuleAsync(Guid userId, Guid id)
    {
        if (!await _ruleRepository.DeleteAsync(userId, id))
            throw Invalid(nameof(id), "Regra não encontrada.");
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

        var card = NormalizeDetail(request.Card);
        var category = NormalizeDetail(request.Category);
        var tagId = await ResolveTagAsync(record.UserId, title, category);

        var entry = await _entryService.CreateEntry(record.UserId, new CreateEntryRequest
        {
            Title = title,
            Value = value,
            Kind = kind,
            Description = BuildDescription(card, category),
            TagId = tagId,
            Date = date
        });

        record.LastFingerprint = fingerprint;
        record.LastCaptureAt = nowUtc;
        await _keyRepository.UpsertAsync(record);

        return new CaptureEntryResponse
        {
            Created = true,
            EntryId = entry.Id,
            TagId = entry.TagId,
            Description = entry.Description,
            Title = entry.Title,
            Value = entry.Value,
            Kind = entry.Kind,
            Date = entry.Date
        };
    }

    /// <summary>Regras do usuário primeiro; depois, uma tag cujo nome combine com a categoria.</summary>
    private async Task<Guid?> ResolveTagAsync(Guid userId, string title, string? category)
    {
        var haystack = Normalize($"{title} {category}");

        var rules = await _ruleRepository.GetByUserAsync(userId);
        foreach (var rule in rules)
        {
            var needle = Normalize(rule.Match);
            if (needle.Length > 0 && haystack.Contains(needle, StringComparison.Ordinal))
                return rule.TagId;
        }

        var normalizedCategory = Normalize(category ?? string.Empty);
        if (normalizedCategory.Length == 0)
            return null;

        var tags = (await _tagRepository.GetByUserAsync(userId)).ToList();

        var exact = tags.FirstOrDefault(t => Normalize(t.Name) == normalizedCategory);
        if (exact is not null)
            return exact.Id;

        var partial = tags.FirstOrDefault(t =>
        {
            var name = Normalize(t.Name);
            return name.Length >= 3
                && (normalizedCategory.Contains(name, StringComparison.Ordinal)
                    || name.Contains(normalizedCategory, StringComparison.Ordinal));
        });

        return partial?.Id;
    }

    private static string BuildDescription(string? card, string? category)
    {
        var parts = new List<string> { "Capturado automaticamente" };
        if (card is not null)
            parts.Add($"Cartão: {card}");
        if (category is not null)
            parts.Add($"Categoria: {category}");

        return string.Join(" · ", parts);
    }

    private static string? NormalizeDetail(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        return trimmed.Length > MaxDetailLength ? trimmed[..MaxDetailLength] : trimmed;
    }

    /// <summary>Minúsculas, sem acentos e com espaços simples, para comparar textos.</summary>
    internal static string Normalize(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static CaptureRuleResponse ToResponse(CaptureRule rule)
    {
        return new CaptureRuleResponse { Id = rule.Id, Match = rule.Match, TagId = rule.TagId };
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
