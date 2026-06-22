using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using MonthlyExpenseCategoryEntity = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;

namespace Zeno.Infrastructure.SQL.Repositories;

public class MonthlyExpenseCategoryRepository : IMonthlyExpenseCategoryRepository
{
    private readonly ZenoMongoContext _context;

    public MonthlyExpenseCategoryRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<MonthlyExpenseCategoryEntity?> GetByIdAsync(Guid id)
    {
        return await _context.MonthlyExpenseCategories.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MonthlyExpenseCategoryEntity>> GetByUserAsync(Guid userId)
    {
        return await _context.MonthlyExpenseCategories
            .Find(x => x.UserId == userId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<MonthlyExpenseCategoryEntity> CreateAsync(MonthlyExpenseCategoryEntity category)
    {
        await _context.MonthlyExpenseCategories.InsertOneAsync(category);
        return category;
    }

    public async Task<MonthlyExpenseCategoryEntity> UpdateAsync(MonthlyExpenseCategoryEntity category)
    {
        var filter = Builders<MonthlyExpenseCategoryEntity>.Filter.Eq(x => x.Id, category.Id);
        await _context.MonthlyExpenseCategories.ReplaceOneAsync(filter, category);
        return category;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.MonthlyExpenseCategories.DeleteOneAsync(x => x.Id == id);
    }
}
