using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Capture;
using Zeno.Application.Responses.Capture;
using Zeno.Application.Responses.Common;

namespace Zeno.Controllers;

[ApiController]
[Route("api/capture")]
public class CaptureController : AppControllerBase
{
    private const string KeyHeader = "X-Capture-Key";

    private readonly ICaptureService _service;

    public CaptureController(ICaptureService service)
    {
        _service = service;
    }

    /// <summary>Se o usuário já gerou uma chave de captura (o valor nunca é devolvido de novo).</summary>
    [HttpGet("key")]
    public async Task<IActionResult> GetKeyStatus()
    {
        var result = await _service.GetKeyStatusAsync(GetUserId());
        return Ok(ApiResponse<CaptureKeyStatusResponse>.Ok(result));
    }

    /// <summary>Gera uma nova chave (substitui a anterior) e a devolve uma única vez.</summary>
    [HttpPost("key")]
    public async Task<IActionResult> CreateKey()
    {
        var result = await _service.CreateKeyAsync(GetUserId());
        return Ok(ApiResponse<CaptureKeyResponse>.Ok(result));
    }

    [HttpDelete("key")]
    public async Task<IActionResult> RevokeKey()
    {
        await _service.RevokeKeyAsync(GetUserId());
        return NoContent();
    }

    /// <summary>Cria um lançamento a partir de uma automação. Autenticado pelo header X-Capture-Key.</summary>
    [AllowAnonymous]
    [HttpPost("entry")]
    public async Task<IActionResult> CaptureEntry([FromBody] CaptureEntryRequest request, [FromQuery] string? tz)
    {
        Request.Headers.TryGetValue(KeyHeader, out var key);

        return await HandleAsync(
            () => _service.CaptureAsync(key.ToString(), request, tz),
            captured => captured is null
                ? (IActionResult)Unauthorized()
                : Ok(ApiResponse<CaptureEntryResponse>.Ok(captured)));
    }
}
