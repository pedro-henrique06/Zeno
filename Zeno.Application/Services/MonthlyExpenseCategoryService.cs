using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.MonthlyExpenseCategories;
using Zeno.Domain.Interfaces;
using MonthlyExpenseCategoryEntity = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;

namespace Zeno.Application.Services;

public class MonthlyExpenseCategoryService : IMonthlyExpenseCategoryService
{
    private readonly IValidator<CreateMonthlyExpenseCategoryRequest> _createValidator;
    private readonly IValidator<UpdateMonthlyExpenseCategoryRequest> _updateValidator;
    private readonly IMonthlyExpenseCategoryRepository _repository;

    public MonthlyExpenseCategoryService(
        IValidator<CreateMonthlyExpenseCategoryRequest> createValidator,
        IValidator<UpdateMonthlyExpenseCategoryRequest> updateValidator,
        IMonthlyExpenseCategoryRepository repository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _repository = repository;
    }

    public async Task<IEnumerable<MonthlyExpenseCategoryEntity>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task<MonthlyExpenseCategoryEntity?> GetByIdAsync(Guid userId, Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        return category is not null && category.UserId == userId ? category : null;
    }

    public async Task<MonthlyExpenseCategoryEntity> CreateAsync(Guid userId, CreateMonthlyExpenseCategoryRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var category = new MonthlyExpenseCategoryEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(category);
        return category;
    }

    public async Task UpdateAsync(Guid userId, UpdateMonthlyExpenseCategoryRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Gasto mensal não encontrado.")
                }));

        existing.Name = request.Name;
        existing.Amount = request.Amount;
        await _repository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Gasto mensal não encontrado.")
                }));

        await _repository.DeleteAsync(id);
    }
}
