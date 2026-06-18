using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Responses.Common;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : AppControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        var result = await _dashboardService.GetMonthlySummaryAsync(userId, month, year);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategorySummary([FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        var result = await _dashboardService.GetCategorySummaryAsync(userId, month, year);
        return Ok(ApiResponse<object>.Ok(result));
    }
}