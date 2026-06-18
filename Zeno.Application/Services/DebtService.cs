using Zeno.Application.Exceptions;
using Zeno.Application.Requests;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class DebtService : Zeno.Application.Interfaces.IDebtService
{
    private readonly IDebtRepository _repository;

    public DebtService(IDebtRepository repository)
    {
        _repository = repository;
    }

    public async Task<Zeno.Domain.Debt.Debt> CreateAsync(Guid userId, CreateDebtRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Name", "Nome é obrigatório.") }));

        if (request.TotalAmount <= 0)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("TotalAmount", "Valor total deve ser maior que zero.") }));

        if (request.PaidAmount > request.TotalAmount)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("PaidAmount", "Valor pago não pode ser maior que o total.") }));

        if (request.MonthlyPayment < 0)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("MonthlyPayment", "Pagamento mensal não pode ser negativo.") }));

        var debt = new Zeno.Domain.Debt.Debt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            TotalAmount = request.TotalAmount,
            PaidAmount = request.PaidAmount,
            MonthlyPayment = request.MonthlyPayment,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(debt);
    }

    public async Task<IEnumerable<Zeno.Domain.Debt.Debt>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task<Zeno.Domain.Debt.Debt?> GetByIdAsync(Guid userId, Guid id)
    {
        var debt = await _repository.GetByIdAsync(id);
        if (debt is null || debt.UserId != userId)
            return null;
        return debt;
    }

    public async Task<Zeno.Domain.Debt.Debt> UpdateAsync(Guid userId, UpdateDebtRequest request)
    {
        var debt = await _repository.GetByIdAsync(request.Id);
        if (debt is null || debt.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Dívida não encontrada.") }));

        debt.Name = request.Name;
        debt.TotalAmount = request.TotalAmount;
        debt.PaidAmount = request.PaidAmount;
        debt.MonthlyPayment = request.MonthlyPayment;

        return await _repository.UpdateAsync(debt);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var debt = await _repository.GetByIdAsync(id);
        if (debt is null || debt.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Dívida não encontrada.") }));

        await _repository.DeleteAsync(id);
    }

    public async Task<Zeno.Application.Responses.PayoffSimulationResponse> GetPayoffSimulationAsync(Guid userId, Guid id)
    {
        var debt = await _repository.GetByIdAsync(id);
        if (debt is null || debt.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Dívida não encontrada.") }));

        var remainingAmount = debt.TotalAmount - debt.PaidAmount;
        var estimatedMonths = 0;
        string alertMessage;

        if (debt.PaidAmount >= debt.TotalAmount)
        {
            alertMessage = "Dívida quitada!";
        }
        else if (debt.MonthlyPayment <= 0)
        {
            alertMessage = "Pagamento mensal zerado ou inválido.";
        }
        else
        {
            estimatedMonths = (int)Math.Ceiling(remainingAmount / debt.MonthlyPayment);
            alertMessage = string.Empty;
        }

        return new Zeno.Application.Responses.PayoffSimulationResponse
        {
            TotalAmount = debt.TotalAmount,
            PaidAmount = debt.PaidAmount,
            RemainingAmount = remainingAmount,
            MonthlyPayment = debt.MonthlyPayment,
            EstimatedMonthsToPayOff = estimatedMonths,
            AlertMessage = alertMessage
        };
    }

    public async Task<Zeno.Application.Responses.DebtSummaryResponse> GetSummaryAsync(Guid userId)
    {
        var debts = (await _repository.GetByUserAsync(userId)).ToList();

        var totalDebt = debts.Sum(d => d.TotalAmount);
        var totalPaid = debts.Sum(d => d.PaidAmount);
        var totalRemaining = debts.Sum(d => d.TotalAmount - d.PaidAmount);
        var averageMonthlyPayment = debts.Count > 0 ? debts.Average(d => d.MonthlyPayment) : 0;
        var totalMonthlyPayment = debts.Sum(d => d.MonthlyPayment);

        var estimatedMonths = 0;
        if (totalMonthlyPayment > 0 && totalRemaining > 0)
        {
            estimatedMonths = (int)Math.Ceiling(totalRemaining / totalMonthlyPayment);
        }

        return new Zeno.Application.Responses.DebtSummaryResponse
        {
            TotalDebt = totalDebt,
            TotalPaid = totalPaid,
            TotalRemaining = totalRemaining,
            AverageMonthlyPayment = Math.Round(averageMonthlyPayment, 2),
            EstimatedMonthsToBecomeDebtFree = estimatedMonths
        };
    }
}