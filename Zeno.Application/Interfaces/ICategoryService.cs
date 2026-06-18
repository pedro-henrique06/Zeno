using Zeno.Application.Requests;
using Zeno.Domain.CustomCategory;

namespace Zeno.Application.Interfaces;

public interface ICategoryService
{
    Task<Category> CreateAsync(Guid userId, CreateCategoryRequest request);
    Task<IEnumerable<Category>> GetAllAsync(Guid userId);
    Task<Category?> GetByIdAsync(Guid userId, Guid id);
    Task<Category> UpdateAsync(Guid userId, UpdateCategoryRequest request);
    Task DeleteAsync(Guid userId, Guid id);
}

public interface ICategoryRuleService
{
    Task<CategoryRule> CreateAsync(Guid userId, CreateCategoryRuleRequest request);
    Task<IEnumerable<CategoryRule>> GetAllAsync(Guid userId);
    Task<CategoryRule?> GetByIdAsync(Guid userId, Guid id);
    Task<CategoryRule> UpdateAsync(Guid userId, UpdateCategoryRuleRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task<Category?> ApplyRuleAsync(Guid userId, string description);
}