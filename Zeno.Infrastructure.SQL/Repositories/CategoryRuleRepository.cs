using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using CategoryRuleEntity = Zeno.Domain.CustomCategory.CategoryRule;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CategoryRuleRepository : ICategoryRuleRepository
{
    private readonly ZenoDbContext _context;

    public CategoryRuleRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryRuleEntity?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, keyword, categoryid, createdat
                             FROM category_rules WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToCategoryRule(row);
    }

    public async Task<IEnumerable<CategoryRuleEntity>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, userid, keyword, categoryid, createdat
                             FROM category_rules WHERE userid = @UserId ORDER BY keyword";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToCategoryRule(r)).Cast<CategoryRuleEntity>();
    }

    public async Task<CategoryRuleEntity?> FindMatchAsync(Guid userId, string description)
    {
        var sql = @"SELECT id, userid, keyword, categoryid, createdat
                    FROM category_rules
                    WHERE userid = @UserId
                      AND LOWER(@Description) LIKE '%' || LOWER(keyword) || '%'
                    LIMIT 1";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId, Description = description });
        return row is null ? null : MapToCategoryRule(row);
    }

    public async Task<CategoryRuleEntity> CreateAsync(CategoryRuleEntity rule)
    {
        const string sql = @"INSERT INTO category_rules (id, userid, keyword, categoryid, createdat)
                             VALUES (@Id, @UserId, @Keyword, @CategoryId, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            rule.Id,
            rule.UserId,
            rule.Keyword,
            rule.CategoryId,
            rule.CreatedAt
        });
        return rule;
    }

    public async Task<CategoryRuleEntity> UpdateAsync(CategoryRuleEntity rule)
    {
        const string sql = @"UPDATE category_rules SET keyword = @Keyword, categoryid = @CategoryId WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            rule.Id,
            rule.Keyword,
            rule.CategoryId
        });
        return rule;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM category_rules WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private static CategoryRuleEntity MapToCategoryRule(dynamic row)
    {
        return new CategoryRuleEntity
        {
            Id = row.id,
            UserId = row.userid,
            Keyword = row.keyword,
            CategoryId = row.categoryid,
            CreatedAt = row.createdat
        };
    }
}