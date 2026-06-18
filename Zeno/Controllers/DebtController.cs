using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Common;
using Zeno.Domain.Debt;

namespace Zeno.Controllers;

[ApiController]
[Route("api/debts")]
public class DebtController : AppControllerBase
{
    private readonly IDebtService _service;

    public DebtController(IDebtService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<object>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDebtRequest request)
    {
        var userId = GetUserId();
        var data = await _service.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<object>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateDebtRequest request)
    {
        var userId = GetUserId();
        await _service.UpdateAsync(userId, request);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        await _service.DeleteAsync(userId, id);
        return NoContent();
    }

    [HttpGet("{id:guid}/payoff-simulation")]
    public async Task<IActionResult> GetPayoffSimulation(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetPayoffSimulationAsync(userId, id);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();
        var result = await _service.GetSummaryAsync(userId);
        return Ok(ApiResponse<object>.Ok(result));
    }
}