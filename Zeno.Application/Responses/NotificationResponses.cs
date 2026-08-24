using Zeno.Domain.Enum;

namespace Zeno.Application.Responses;

public class NotificationPreferenceResponse
{
    public bool DailyEnabled { get; set; }
    public int SendHour { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public DateOnly? LastSentOn { get; set; }

    /// <summary>Quantos aparelhos estao aptos a receber push. Zero aqui explica "ativei e nao chega nada".</summary>
    public int ActiveDevices { get; set; }

    /// <summary>False quando o servidor nao tem credencial de push configurada — nenhum envio sai.</summary>
    public bool PushConfigured { get; set; }
}

public class DeviceTokenResponse
{
    public Guid Id { get; set; }
    public DevicePlatform Platform { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

public class SendTestNotificationResponse
{
    public bool PushConfigured { get; set; }
    public int DevicesTargeted { get; set; }
    public int SuccessCount { get; set; }
    public int InvalidTokensRemoved { get; set; }
    public string Message { get; set; } = string.Empty;
}
