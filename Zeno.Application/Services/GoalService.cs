using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Goals;
using Zeno.Application.Responses.Goals;
using Zeno.Domain.Enum;
using Zeno.Domain.Goals;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class GoalService : IGoalService
{
    private readonly IValidator<SaveGoalRequest> _validator;
    private readonly IGoalRepository _goalRepository;
    private readonly IEntryRepository _entryRepository;

    public GoalService(
        IValidator<SaveGoalRequest> validator,
        IGoalRepository goalRepository,
        IEntryRepository entryRepository)
    {
        _validator = validator;
        _goalRepository = goalRepository;
        _entryRepository = entryRepository;
    }

    public async Task<GoalResponse?> GetAsync(Guid userId)
    {
        var goal = await _goalRepository.GetByUserAsync(userId);
        return goal is null ? null : await ToResponseAsync(goal);
    }

    public async Task<GoalResponse> SaveAsync(Guid userId, SaveGoalRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var now = DateTime.UtcNow;
        var existing = await _goalRepository.GetByUserAsync(userId);

        var goal = existing ?? new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = now.Date,
            CreatedAt = now
        };

        goal.Name = request.Name.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.MonthlyContribution = request.MonthlyContribution;
        goal.InitialAmount = request.InitialAmount;
        goal.AnnualRatePercent = request.AnnualRatePercent;
        goal.UpdatedAt = now;

        await _goalRepository.UpsertAsync(goal);
        return await ToResponseAsync(goal);
    }

    public Task DeleteAsync(Guid userId)
    {
        return _goalRepository.DeleteByUserAsync(userId);
    }

    private async Task<GoalResponse> ToResponseAsync(Goal goal)
    {
        var saved = goal.InitialAmount + await SumSavingsSinceAsync(goal.UserId, goal.StartDate);
        var progress = goal.TargetAmount > 0
            ? Math.Min(Math.Round(saved / goal.TargetAmount * 100m, 1), 100m)
            : 0m;

        return new GoalResponse
        {
            Id = goal.Id,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            MonthlyContribution = goal.MonthlyContribution,
            InitialAmount = goal.InitialAmount,
            AnnualRatePercent = goal.AnnualRatePercent,
            StartDate = goal.StartDate,
            SavedAmount = saved,
            ProgressPercent = Math.Max(progress, 0m)
        };
    }

    /// <summary>Soma os lançamentos de Economia (incluindo recorrências) de startDate até hoje.</summary>
    private async Task<decimal> SumSavingsSinceAsync(Guid userId, DateTime startDate)
    {
        var rangeStart = startDate.Date;
        var rangeEnd = DateTime.UtcNow.Date.AddDays(1);
        if (rangeEnd <= rangeStart)
            return 0m;

        var entries = await _entryRepository.GetByUserInRangeAsync(userId, rangeStart, rangeEnd);
        var templates = await _entryRepository.GetRecurringBeforeAsync(userId, rangeEnd);
        var occurrences = RecurringEntryProjector.ExpandOccurrencesInRange(templates, rangeStart, rangeEnd);

        return entries
            .Concat(occurrences)
            .Where(e => e.Kind == EntryKind.Economia)
            .Sum(e => e.Value);
    }
}
