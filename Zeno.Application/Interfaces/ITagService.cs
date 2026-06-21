using Zeno.Application.Requests.Tags;
using TagEntity = Zeno.Domain.Tag.Tag;

namespace Zeno.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagEntity>> GetAllAsync(Guid userId);
    Task<TagEntity?> GetByIdAsync(Guid userId, Guid id);
    Task<TagEntity> CreateAsync(Guid userId, CreateTagRequest request);
    Task UpdateAsync(Guid userId, UpdateTagRequest request);
    Task DeleteAsync(Guid userId, Guid id);
}
