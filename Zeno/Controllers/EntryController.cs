using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Responses.Common;
using Zeno.Domain.Entry;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntryController : AppControllerBase
{
    private readonly IEntryService _entryService;

    public EntryController(IEntryService entryService)
    {
        _entryService = entryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByMonth([FromQuery] GetEntriesByMonthQuery query)
    {
        var userId = GetUserId();
        var result = await _entryService.GetEntriesByMonth(userId, query);
        return Ok(ApiResponse<PagedResponse<Entry>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEntryRequest request)
    {
        var userId = GetUserId();
        var data = await _entryService.CreateEntry(userId, request);
        return CreatedAtAction(nameof(GetByMonth), new { id = data.Id }, ApiResponse<Entry>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateEntryRequest request)
    {
        var userId = GetUserId();
        await _entryService.UpdateEntry(userId, request);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        await _entryService.DeleteEntry(userId, new DeleteEntryRequest { Id = id });
        return NoContent();
    }
}