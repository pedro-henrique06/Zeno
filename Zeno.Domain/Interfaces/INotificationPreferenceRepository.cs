using Zeno.Domain.Notification;

namespace Zeno.Domain.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByUserAsync(Guid userId);
    Task<NotificationPreference> UpsertAsync(NotificationPreference preference);

    /// <summary>Todas as preferencias com o resumo diario ligado. O filtro de horario e feito em memoria porque depende do fuso de cada usuario.</summary>
    Task<IEnumerable<NotificationPreference>> GetAllEnabledAsync();

    Task MarkSentAsync(Guid userId, DateOnly localDate);
}
