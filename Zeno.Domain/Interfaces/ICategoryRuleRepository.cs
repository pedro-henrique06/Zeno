using CategoryRuleEntity = Zeno.Domain.CustomCategory.CategoryRule;

namespace Zeno.Domain.Interfaces;

public interface ICategoryRuleRepository
{
    Task<CategoryRuleEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<CategoryRuleEntity>> GetByUserAsync(Guid userId);
    Task<CategoryRuleEntity?> FindMatchAsync(Guid userId, string description);
    Task<CategoryRuleEntity> CreateAsync(CategoryRuleEntity rule);
    Task<CategoryRuleEntity> UpdateAsync(CategoryRuleEntity rule);
    Task DeleteAsync(Guid id);
}