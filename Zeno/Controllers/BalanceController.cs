using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Balances;
using Zeno.Application.Responses.Common;

namespace Zeno.Controllers;

[ApiController]
[Route("api/balances")]
public class BalanceController : AppControllerBase
{
    private readonly IBalanceService _service;

    public BalanceController(IBalanceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonthly([FromQuery] MonthQuery query)
    {
        var userId = GetUserId();
        var result = await _service.GetMonthlyBalances(userId, query.Month, query.Year);
        return Ok(ApiResponse<BalancesResponse>.Ok(result));
    }
}
