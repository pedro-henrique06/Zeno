using Zeno.Domain.Notification;

namespace Zeno.Domain.Interfaces;

public interface IDeviceTokenRepository
{
    Task<IEnumerable<DeviceToken>> GetActiveByUserAsync(Guid userId);
    Task<DeviceToken?> GetByTokenAsync(string token);

    /// <summary>Cria o token ou, se ele ja existir, o reatribui ao usuario atual e reativa.</summary>
    Task<DeviceToken> UpsertAsync(DeviceToken deviceToken);

    Task DeactivateAsync(string token);
    Task DeleteByUserAndTokenAsync(Guid userId, string token);
}
