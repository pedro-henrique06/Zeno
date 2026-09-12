using TagEntity = Zeno.Domain.Tag.Tag;

namespace Zeno.Domain.Interfaces;

public interface ITagRepository
{
    Task<TagEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<TagEntity>> GetByUserAsync(Guid userId);
    Task<TagEntity> CreateAsync(TagEntity tag);
    Task<TagEntity> UpdateAsync(TagEntity tag);
    Task DeleteAsync(Guid id);
}
