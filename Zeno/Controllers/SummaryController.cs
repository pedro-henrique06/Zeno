using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Common;
using Zeno.Application.Responses.Summary;

namespace Zeno.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController : AppControllerBase
{
    private readonly ISummaryService _service;

    public SummaryController(ISummaryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonthly([FromQuery] MonthQuery query)
    {
        var userId = GetUserId();
        var result = await _service.GetMonthlySummary(userId, query.Month, query.Year);
        return Ok(ApiResponse<SummaryResponse>.Ok(result));
    }
}
