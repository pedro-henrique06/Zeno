using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Common;
using DomainPushSubscription = Zeno.Domain.Push.PushSubscription;

namespace Zeno.Controllers;

public sealed record SubscribeRequest(string Endpoint, string P256dh, string Auth);
public sealed record UnsubscribeRequest(string Endpoint);

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController : AppControllerBase
{
    private readonly IPushNotificationService _pushService;

    public PushController(IPushNotificationService pushService)
    {
        _pushService = pushService;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                var sub = new DomainPushSubscription
                {
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    P256dh = request.P256dh,
                    Auth = request.Auth,
                };
                await _pushService.SubscribeAsync(userId, sub);
                return true;
            },
            _ => Ok(ApiResponse<bool>.Ok(true)));
    }

    [HttpDelete("subscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            async () =>
            {
                await _pushService.UnsubscribeAsync(userId, request.Endpoint);
                return true;
            },
            _ => Ok(ApiResponse<bool>.Ok(true)));
    }
}
