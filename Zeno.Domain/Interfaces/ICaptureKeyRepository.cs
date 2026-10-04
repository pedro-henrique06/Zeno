using Zeno.Domain.Capture;

namespace Zeno.Domain.Interfaces;

public interface ICaptureKeyRepository
{
    Task<CaptureKey?> GetByUserAsync(Guid userId);
    Task<CaptureKey?> GetByHashAsync(string keyHash);
    Task<CaptureKey> UpsertAsync(CaptureKey key);
    Task DeleteByUserAsync(Guid userId);
}
