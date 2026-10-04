using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Houses;
using Zeno.Application.Responses.Common;
using Zeno.Application.Responses.Houses;

namespace Zeno.Controllers;

[ApiController]
[Route("api/houses")]
public class HouseBudgetController : AppControllerBase
{
    private readonly IHouseBudgetService _service;

    public HouseBudgetController(IHouseBudgetService service)
    {
        _service = service;
    }

    /// <summary>Orçamento 50/30/20 da casa no mês (padrão: mês atual).</summary>
    [HttpGet("{id:guid}/budget")]
    public async Task<IActionResult> GetBudget(Guid id, [FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.GetBudgetAsync(userId, id, month, year),
            data => Ok(ApiResponse<HouseBudgetResponse>.Ok(data)));
    }

    /// <summary>Cria ou substitui a meta da casa (só o dono).</summary>
    [HttpPut("{id:guid}/goal")]
    public async Task<IActionResult> SaveGoal(Guid id, [FromBody] SaveHouseGoalRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.SaveGoalAsync(userId, id, request),
            data => Ok(ApiResponse<HouseGoalResponse?>.Ok(data)));
    }

    [HttpDelete("{id:guid}/goal")]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.DeleteGoalAsync(userId, id),
            NoContent());
    }
}
