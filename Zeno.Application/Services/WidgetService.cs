using System.Security.Cryptography;
using System.Text;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Widgets;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using Zeno.Domain.Widgets;

namespace Zeno.Application.Services;

public class WidgetService : IWidgetService
{
    private const string KeyPrefix = "zw_";

    private readonly IWidgetKeyRepository _keyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISummaryService _summaryService;
    private readonly IGoalService _goalService;
    private readonly IEntryRepository _entryRepository;
    private readonly IClock _clock;

    public WidgetService(
        IWidgetKeyRepository keyRepository,
        IUserRepository userRepository,
        ISummaryService summaryService,
        IGoalService goalService,
        IEntryRepository entryRepository,
        IClock clock)
    {
        _keyRepository = keyRepository;
        _userRepository = userRepository;
        _summaryService = summaryService;
        _goalService = goalService;
        _entryRepository = entryRepository;
        _clock = clock;
    }

    public async Task<WidgetKeyStatusResponse> GetKeyStatusAsync(Guid userId)
    {
        var existing = await _keyRepository.GetByUserAsync(userId);
        return new WidgetKeyStatusResponse
        {
            Enabled = existing is not null,
            CreatedAt = existing?.CreatedAt
        };
    }

    public async Task<WidgetKeyResponse> CreateKeyAsync(Guid userId)
    {
        var plainKey = KeyPrefix + ToBase64Url(RandomNumberGenerator.GetBytes(32));
        var existing = await _keyRepository.GetByUserAsync(userId);

        var record = existing ?? new WidgetKey { Id = Guid.NewGuid(), UserId = userId };
        record.KeyHash = Hash(plainKey);
        record.CreatedAt = _clock.UtcNow;

        await _keyRepository.UpsertAsync(record);

        return new WidgetKeyResponse { Key = plainKey, CreatedAt = record.CreatedAt };
    }

    public Task RevokeKeyAsync(Guid userId)
    {
        return _keyRepository.DeleteByUserAsync(userId);
    }

    public async Task<WidgetSummaryResponse?> GetSummaryAsync(string? key, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var record = await _keyRepository.GetByHashAsync(Hash(key.Trim()));
        if (record is null)
            return null;

        var user = await _userRepository.GetByIdAsync(record.UserId);
        if (user is null)
            return null;

        var nowUtc = _clock.UtcNow;
        var timeZone = ResolveTimeZone(timeZoneId);
        var today = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone).Date;

        var summary = await _summaryService.GetMonthlySummary(user.Id, today.Month, today.Year);
        var spentToday = await SumDailySpendAsync(user.Id, today);
        var goal = await _goalService.GetAsync(user.Id);

        return new WidgetSummaryResponse
        {
            Currency = user.Currency.ToString(),
            Language = user.Language.ToString(),
            Month = today.Month,
            Year = today.Year,
            MonthBalance = summary.Performance,
            Income = summary.Movements.Entrada,
            Expenses = summary.Movements.Saida + summary.Movements.Diario + summary.Movements.Cartao,
            DailyBudget = summary.DailyBudget,
            SpentToday = spentToday,
            AvailableToday = summary.DailyBudget - spentToday,
            Goal = goal is null
                ? null
                : new WidgetGoalResponse
                {
                    Name = goal.Name,
                    ProgressPercent = goal.ProgressPercent,
                    SavedAmount = goal.SavedAmount,
                    TargetAmount = goal.TargetAmount
                },
            GeneratedAtUtc = nowUtc
        };
    }

    private async Task<decimal> SumDailySpendAsync(Guid userId, DateTime day)
    {
        var rangeEnd = day.AddDays(1);
        var entries = await _entryRepository.GetByUserInRangeAsync(userId, day, rangeEnd);
        var templates = await _entryRepository.GetRecurringBeforeAsync(userId, rangeEnd);
        var occurrences = RecurringEntryProjector.ExpandOccurrencesInRange(templates, day, rangeEnd);

        return entries
            .Concat(occurrences)
            .Where(e => e.Kind == EntryKind.Diario)
            .Sum(e => e.Value);
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? NotificationPreference.DefaultTimeZoneId : timeZoneId;
        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : TimeZoneInfo.Utc;
    }

    private static string Hash(string key)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
