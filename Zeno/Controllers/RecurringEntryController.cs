using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Common;
using Zeno.Domain.Recurring;

namespace Zeno.Controllers;

[ApiController]
[Route("api/recurring-entries")]
public class RecurringEntryController : AppControllerBase
{
    private readonly IRecurringEntryService _service;

    public RecurringEntryController(IRecurringEntryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<IEnumerable<RecurringEntry>>.Ok(result));
    }

    [HttpGet("wallet/{walletId:guid}")]
    public async Task<IActionResult> GetByWallet(Guid walletId)
    {
        var userId = GetUserId();
        var result = await _service.GetByWalletAsync(userId, walletId);
        return Ok(ApiResponse<IEnumerable<RecurringEntry>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<RecurringEntry>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringEntryRequest request)
    {
        var userId = GetUserId();
        var data = await _service.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<RecurringEntry>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRecurringEntryRequest request)
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
