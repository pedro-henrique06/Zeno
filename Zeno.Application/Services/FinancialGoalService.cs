using Zeno.Application.Exceptions;
using Zeno.Application.Requests;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class FinancialGoalService : Zeno.Application.Interfaces.IFinancialGoalService
{
    private readonly IFinancialGoalRepository _repository;

    public FinancialGoalService(IFinancialGoalRepository repository)
    {
        _repository = repository;
    }

    public async Task<Zeno.Domain.FinancialGoal.FinancialGoal> CreateAsync(Guid userId, CreateFinancialGoalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Name", "Nome é obrigatório.") }));

        if (request.TargetAmount <= 0)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("TargetAmount", "Valor alvo deve ser maior que zero.") }));

        if (request.CurrentAmount < 0)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("CurrentAmount", "Valor atual não pode ser negativo.") }));

        var goal = new Zeno.Domain.FinancialGoal.FinancialGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            TargetDate = request.TargetDate,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(goal);
    }

    public async Task<IEnumerable<Zeno.Domain.FinancialGoal.FinancialGoal>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task<Zeno.Domain.FinancialGoal.FinancialGoal?> GetByIdAsync(Guid userId, Guid id)
    {
        var goal = await _repository.GetByIdAsync(id);
        if (goal is null || goal.UserId != userId)
            return null;
        return goal;
    }

    public async Task<Zeno.Domain.FinancialGoal.FinancialGoal> UpdateAsync(Guid userId, UpdateFinancialGoalRequest request)
    {
        var goal = await _repository.GetByIdAsync(request.Id);
        if (goal is null || goal.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Meta não encontrada.") }));

        goal.Name = request.Name;
        goal.TargetAmount = request.TargetAmount;
        goal.CurrentAmount = request.CurrentAmount;
        goal.TargetDate = request.TargetDate;

        return await _repository.UpdateAsync(goal);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var goal = await _repository.GetByIdAsync(id);
        if (goal is null || goal.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Meta não encontrada.") }));

        await _repository.DeleteAsync(id);
    }

    public async Task<Zeno.Application.Responses.GoalSimulationResponse> GetSimulationAsync(Guid userId, Guid id)
    {
        var goal = await _repository.GetByIdAsync(id);
        if (goal is null || goal.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Meta não encontrada.") }));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingAmount = goal.TargetAmount - goal.CurrentAmount;
        var monthsRemaining = 0;
        var requiredMonthlySaving = 0m;
        string alertMessage;

        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            alertMessage = "Meta já alcançada!";
        }
        else if (goal.TargetDate < today)
        {
            alertMessage = "Data alvo já passou.";
            var daysDiff = today.DayNumber - goal.TargetDate.DayNumber;
            monthsRemaining = Math.Max(1, daysDiff / 30);
            if (monthsRemaining > 0)
                requiredMonthlySaving = remainingAmount / monthsRemaining;
        }
        else
        {
            var daysDiff = goal.TargetDate.DayNumber - today.DayNumber;
            monthsRemaining = Math.Max(1, daysDiff / 30);
            requiredMonthlySaving = remainingAmount / monthsRemaining;
            alertMessage = string.Empty;
        }

        return new Zeno.Application.Responses.GoalSimulationResponse
        {
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            RemainingAmount = remainingAmount,
            MonthsRemaining = monthsRemaining,
            RequiredMonthlySaving = Math.Round(requiredMonthlySaving, 2),
            AlertMessage = alertMessage
        };
    }
}