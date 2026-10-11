using Zeno.Domain.Capture;

namespace Zeno.Domain.Interfaces;

public interface ICaptureCardRepository
{
    Task<IReadOnlyList<CaptureCard>> GetByUserAsync(Guid userId);
    Task<CaptureCard?> GetByIdAsync(Guid userId, Guid id);
    Task<CaptureCard?> GetByNameKeyAsync(Guid userId, string nameKey);
    Task UpsertAsync(CaptureCard card);
    Task DeleteByUserAsync(Guid userId);
}
