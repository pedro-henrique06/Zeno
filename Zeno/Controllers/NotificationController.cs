using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Notifications;
using Zeno.Application.Responses;
using Zeno.Application.Responses.Common;

namespace Zeno.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationController : AppControllerBase
{
    private readonly INotificationService _service;

    public NotificationController(INotificationService service)
    {
        _service = service;
    }

    /// <summary>Registra (ou reativa) o token de push do aparelho para o usuario autenticado.</summary>
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.RegisterDeviceAsync(userId, request),
            data => Ok(ApiResponse<DeviceTokenResponse>.Ok(data)));
    }

    [HttpDelete("devices/{token}")]
    public async Task<IActionResult> UnregisterDevice(string token)
    {
        var userId = GetUserId();
        await _service.UnregisterDeviceAsync(userId, token);
        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        var result = await _service.GetPreferenceAsync(userId);
        return Ok(ApiResponse<NotificationPreferenceResponse>.Ok(result));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferenceRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.UpdatePreferenceAsync(userId, request),
            data => Ok(ApiResponse<NotificationPreferenceResponse>.Ok(data)));
    }

    /// <summary>Dispara um push imediato para os aparelhos do usuario. Serve para diagnosticar "ativei e nao chega nada".</summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTest()
    {
        var userId = GetUserId();
        var result = await _service.SendTestAsync(userId);
        return Ok(ApiResponse<SendTestNotificationResponse>.Ok(result));
    }
}
