using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using CategoryRuleEntity = Zeno.Domain.CustomCategory.CategoryRule;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CategoryRuleRepository : ICategoryRuleRepository
{
    private readonly ZenoMongoContext _context;

    public CategoryRuleRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<CategoryRuleEntity?> GetByIdAsync(Guid id)
    {
        return await _context.CategoryRules.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CategoryRuleEntity>> GetByUserAsync(Guid userId)
    {
        return await _context.CategoryRules
            .Find(x => x.UserId == userId)
            .SortBy(x => x.Keyword)
            .ToListAsync();
    }

    public async Task<CategoryRuleEntity?> FindMatchAsync(Guid userId, string description)
    {
        var rules = await _context.CategoryRules
            .Find(x => x.UserId == userId)
            .ToListAsync();

        return rules.FirstOrDefault(r => 
            description.Contains(r.Keyword, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<CategoryRuleEntity> CreateAsync(CategoryRuleEntity rule)
    {
        await _context.CategoryRules.InsertOneAsync(rule);
        return rule;
    }

    public async Task<CategoryRuleEntity> UpdateAsync(CategoryRuleEntity rule)
    {
        var filter = Builders<CategoryRuleEntity>.Filter.Eq(x => x.Id, rule.Id);
        await _context.CategoryRules.ReplaceOneAsync(filter, rule);
        return rule;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.CategoryRules.DeleteOneAsync(x => x.Id == id);
    }
}
