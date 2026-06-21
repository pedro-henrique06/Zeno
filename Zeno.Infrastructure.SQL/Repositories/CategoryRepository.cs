using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using CategoryEntity = Zeno.Domain.CustomCategory.Category;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ZenoMongoContext _context;

    public CategoryRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<CategoryEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Categories.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CategoryEntity>> GetAllAsync()
    {
        return await _context.Categories
            .Find(_ => true)
            .SortBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<CategoryEntity>> GetByUserAsync(Guid userId)
    {
        return await _context.Categories
            .Find(x => x.UserId == userId)
            .SortBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<CategoryEntity>> GetGlobalAsync()
    {
        return await _context.Categories
            .Find(x => x.UserId == null)
            .SortBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<CategoryEntity> CreateAsync(CategoryEntity category)
    {
        await _context.Categories.InsertOneAsync(category);
        return category;
    }

    public async Task<CategoryEntity> UpdateAsync(CategoryEntity category)
    {
        var filter = Builders<CategoryEntity>.Filter.Eq(x => x.Id, category.Id);
        await _context.Categories.ReplaceOneAsync(filter, category);
        return category;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.Categories.DeleteOneAsync(x => x.Id == id);
    }
}
