using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Homes;
using Zeno.Application.Validators;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class HomeExpenseService : Zeno.Application.Interfaces.IHomeExpenseService
{
    private readonly IValidator<CreateHomeExpenseRequest> _createValidator;
    private readonly IHomeRepository _repository;

    public HomeExpenseService(
        IValidator<CreateHomeExpenseRequest> createValidator,
        IHomeRepository repository)
    {
        _createValidator = createValidator;
        _repository = repository;
    }

    public async Task<HomeExpense> CreateExpense(Guid userId, CreateHomeExpenseRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var isMember = await _repository.IsMemberAsync(request.HomeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.HomeId), "Você não é membro desta casa.")
                }));

        var expense = new HomeExpense
        {
            Id = Guid.NewGuid(),
            HomeId = request.HomeId,
            Title = request.Title,
            Value = request.Value,
            Category = request.Category,
            Month = request.Month,
            Year = request.Year,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateExpenseAsync(expense);
    }

    public async Task DeleteExpense(Guid userId, Guid homeId, Guid expenseId)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(expenseId), "Apenas o administrador pode remover despesas.")
                }));

        await _repository.DeleteExpenseAsync(expenseId);
    }

    public async Task<IEnumerable<HomeExpense>> GetExpensesByMonth(Guid userId, Guid homeId, int month, int year)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        return await _repository.GetExpensesByMonthAsync(homeId, month, year);
    }
}