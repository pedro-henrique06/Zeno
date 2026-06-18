using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Domain.CustomCategory;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Category> CreateAsync(Guid userId, CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Name", "Nome é obrigatório.") }));

        if (request.Type != Category.CategoryType.Income && request.Type != Category.CategoryType.Expense)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Type", "Tipo deve ser 0 (Income) ou 1 (Expense).") }));

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(category);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(Guid userId)
    {
        var global = await _repository.GetGlobalAsync();
        var userCategories = await _repository.GetByUserAsync(userId);
        return global.Concat(userCategories);
    }

    public async Task<Category?> GetByIdAsync(Guid userId, Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category is null)
            return null;

        if (category.UserId.HasValue && category.UserId != userId)
            return null;

        return category;
    }

    public async Task<Category> UpdateAsync(Guid userId, UpdateCategoryRequest request)
    {
        var category = await _repository.GetByIdAsync(request.Id);
        if (category is null || category.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Categoria não encontrada.") }));

        category.Name = request.Name;
        category.Type = request.Type;

        return await _repository.UpdateAsync(category);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category is null || category.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Categoria não encontrada.") }));

        await _repository.DeleteAsync(id);
    }
}

public class CategoryRuleService : ICategoryRuleService
{
    private readonly ICategoryRuleRepository _repository;
    private readonly ICategoryRepository _categoryRepository;

    public CategoryRuleService(ICategoryRuleRepository repository, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryRule> CreateAsync(Guid userId, CreateCategoryRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Keyword", "Palavra-chave é obrigatória.") }));

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("CategoryId", "Categoria não encontrada.") }));

        var rule = new CategoryRule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Keyword = request.Keyword,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(rule);
    }

    public async Task<IEnumerable<CategoryRule>> GetAllAsync(Guid userId)
    {
        return await _repository.GetByUserAsync(userId);
    }

    public async Task<CategoryRule?> GetByIdAsync(Guid userId, Guid id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule is null || rule.UserId != userId)
            return null;
        return rule;
    }

    public async Task<CategoryRule> UpdateAsync(Guid userId, UpdateCategoryRuleRequest request)
    {
        var rule = await _repository.GetByIdAsync(request.Id);
        if (rule is null || rule.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Regra não encontrada.") }));

        rule.Keyword = request.Keyword;
        rule.CategoryId = request.CategoryId;

        return await _repository.UpdateAsync(rule);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule is null || rule.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure> { new("Id", "Regra não encontrada.") }));

        await _repository.DeleteAsync(id);
    }

    public async Task<Category?> ApplyRuleAsync(Guid userId, string description)
    {
        var rule = await _repository.FindMatchAsync(userId, description);
        if (rule is null)
            return null;

        return await _categoryRepository.GetByIdAsync(rule.CategoryId);
    }
}