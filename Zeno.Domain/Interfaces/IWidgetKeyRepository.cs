using Zeno.Domain.Widgets;

namespace Zeno.Domain.Interfaces;

public interface IWidgetKeyRepository
{
    Task<WidgetKey?> GetByUserAsync(Guid userId);
    Task<WidgetKey?> GetByHashAsync(string keyHash);
    Task<WidgetKey> UpsertAsync(WidgetKey key);
    Task DeleteByUserAsync(Guid userId);
}
