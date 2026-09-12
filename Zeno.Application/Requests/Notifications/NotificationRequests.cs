using Zeno.Domain.Enum;

namespace Zeno.Application.Requests.Notifications;

public class RegisterDeviceRequest
{
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
}

public class UpdateNotificationPreferenceRequest
{
    public bool DailyEnabled { get; set; }
    public int SendHour { get; set; } = 9;
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
}
