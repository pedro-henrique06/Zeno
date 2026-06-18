using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Common;
using Zeno.Domain.RecurringExpense;

namespace Zeno.Controllers;

[ApiController]
[Route("api/recurring-expenses")]
public class RecurringExpenseController : AppControllerBase
{
    private readonly IRecurringExpenseService _service;

    public RecurringExpenseController(IRecurringExpenseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<IEnumerable<RecurringExpense>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<RecurringExpense>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringExpenseRequest request)
    {
        var userId = GetUserId();
        var data = await _service.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<RecurringExpense>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRecurringExpenseRequest request)
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
}