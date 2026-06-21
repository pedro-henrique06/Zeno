using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using TagEntity = Zeno.Domain.Tag.Tag;

namespace Zeno.Infrastructure.SQL.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ZenoMongoContext _context;

    public TagRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<TagEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Tags.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TagEntity>> GetByUserAsync(Guid userId)
    {
        return await _context.Tags
            .Find(x => x.UserId == userId)
            .SortBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<TagEntity> CreateAsync(TagEntity tag)
    {
        await _context.Tags.InsertOneAsync(tag);
        return tag;
    }

    public async Task<TagEntity> UpdateAsync(TagEntity tag)
    {
        var filter = Builders<TagEntity>.Filter.Eq(x => x.Id, tag.Id);
        await _context.Tags.ReplaceOneAsync(filter, tag);
        return tag;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.Tags.DeleteOneAsync(x => x.Id == id);
    }
}
