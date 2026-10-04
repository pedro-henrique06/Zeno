using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Common;
using Zeno.Application.Responses.Widgets;

namespace Zeno.Controllers;

[ApiController]
[Route("api/widget")]
public class WidgetController : AppControllerBase
{
    private const string KeyHeader = "X-Widget-Key";

    private readonly IWidgetService _service;

    public WidgetController(IWidgetService service)
    {
        _service = service;
    }

    /// <summary>Se o usuário já gerou uma chave de widget (a chave em si nunca é devolvida de novo).</summary>
    [HttpGet("key")]
    public async Task<IActionResult> GetKeyStatus()
    {
        var result = await _service.GetKeyStatusAsync(GetUserId());
        return Ok(ApiResponse<WidgetKeyStatusResponse>.Ok(result));
    }

    /// <summary>Gera uma nova chave (substitui a anterior) e a devolve uma única vez.</summary>
    [HttpPost("key")]
    public async Task<IActionResult> CreateKey()
    {
        var result = await _service.CreateKeyAsync(GetUserId());
        return Ok(ApiResponse<WidgetKeyResponse>.Ok(result));
    }

    [HttpDelete("key")]
    public async Task<IActionResult> RevokeKey()
    {
        await _service.RevokeKeyAsync(GetUserId());
        return NoContent();
    }

    /// <summary>Resumo somente leitura para o widget. Autenticado pelo header X-Widget-Key.</summary>
    [AllowAnonymous]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? tz)
    {
        Request.Headers.TryGetValue(KeyHeader, out var key);
        var result = await _service.GetSummaryAsync(key.ToString(), tz);
        if (result is null)
            return Unauthorized();

        Response.Headers.CacheControl = "no-store";
        return Ok(ApiResponse<WidgetSummaryResponse>.Ok(result));
    }
}
