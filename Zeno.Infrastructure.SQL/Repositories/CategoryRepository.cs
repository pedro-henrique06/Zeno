using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using CategoryEntity = Zeno.Domain.CustomCategory.Category;

namespace Zeno.Infrastructure.SQL.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ZenoDbContext _context;

    public CategoryRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryEntity?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, name, type, createdat
                             FROM categories WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToCategory(row);
    }

    public async Task<IEnumerable<CategoryEntity>> GetAllAsync()
    {
        const string sql = @"SELECT id, userid, name, type, createdat
                             FROM categories ORDER BY name";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql);
        return rows.Select(r => MapToCategory(r)).Cast<CategoryEntity>();
    }

    public async Task<IEnumerable<CategoryEntity>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, userid, name, type, createdat
                             FROM categories WHERE userid = @UserId ORDER BY name";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToCategory(r)).Cast<CategoryEntity>();
    }

    public async Task<IEnumerable<CategoryEntity>> GetGlobalAsync()
    {
        const string sql = @"SELECT id, userid, name, type, createdat
                             FROM categories WHERE userid IS NULL ORDER BY name";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql);
        return rows.Select(r => MapToCategory(r)).Cast<CategoryEntity>();
    }

    public async Task<CategoryEntity> CreateAsync(CategoryEntity category)
    {
        const string sql = @"INSERT INTO categories (id, userid, name, type, createdat)
                             VALUES (@Id, @UserId, @Name, @Type, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.UserId,
            category.Name,
            category.Type,
            category.CreatedAt
        });
        return category;
    }

    public async Task<CategoryEntity> UpdateAsync(CategoryEntity category)
    {
        const string sql = @"UPDATE categories SET name = @Name, type = @Type WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Type
        });
        return category;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM categories WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private static CategoryEntity MapToCategory(dynamic row)
    {
        return new CategoryEntity
        {
            Id = row.id,
            UserId = row.userid,
            Name = row.name,
            Type = (int)row.type,
            CreatedAt = row.createdat
        };
    }
}