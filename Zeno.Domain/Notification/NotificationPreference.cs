namespace Zeno.Domain.Notification;

public class NotificationPreference
{
    public const string DefaultTimeZoneId = "America/Sao_Paulo";
    public const int DefaultSendHour = 9;

    public Guid UserId { get; set; }
    public bool DailyEnabled { get; set; }

    /// <summary>Hora local (0-23) em que o resumo diario deve ser enviado.</summary>
    public int SendHour { get; set; } = DefaultSendHour;

    /// <summary>Fuso IANA usado para interpretar <see cref="SendHour"/>.</summary>
    public string TimeZoneId { get; set; } = DefaultTimeZoneId;

    /// <summary>Ultimo dia local em que o resumo foi enviado, para nao repetir no mesmo dia.</summary>
    public DateOnly? LastSentOn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
