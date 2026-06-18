using CategoryEntity = Zeno.Domain.CustomCategory.Category;

namespace Zeno.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<CategoryEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<CategoryEntity>> GetAllAsync();
    Task<IEnumerable<CategoryEntity>> GetByUserAsync(Guid userId);
    Task<IEnumerable<CategoryEntity>> GetGlobalAsync();
    Task<CategoryEntity> CreateAsync(CategoryEntity category);
    Task<CategoryEntity> UpdateAsync(CategoryEntity category);
    Task DeleteAsync(Guid id);
}