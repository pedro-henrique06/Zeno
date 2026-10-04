using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Goals;
using Zeno.Application.Responses.Common;
using Zeno.Application.Responses.Goals;

namespace Zeno.Controllers;

[ApiController]
[Route("api/goals")]
public class GoalController : AppControllerBase
{
    private readonly IGoalService _service;

    public GoalController(IGoalService service)
    {
        _service = service;
    }

    /// <summary>Meta do usuário autenticado. Data é null quando ainda não existe meta.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetAsync(GetUserId());
        return Ok(ApiResponse<GoalResponse?>.Ok(result));
    }

    /// <summary>Cria ou substitui a meta do usuário.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> Save([FromBody] SaveGoalRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.SaveAsync(userId, request),
            data => Ok(ApiResponse<GoalResponse>.Ok(data)));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> Delete()
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.DeleteAsync(userId),
            NoContent());
    }
}
