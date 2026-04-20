using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
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
    public Task<IActionResult> GetByMonth([FromQuery] int? month, [FromQuery] int? year, [FromQuery] Guid? walletId)
    {
        var query = new GetEntriesByMonthQuery { Month = month, Year = year, WalletId = walletId };
        return HandleAsync(() => _entryService.GetEntriesByMonth(query), data => Ok(data));
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Entry entry)
    {
        return HandleAsync(() => _entryService.CreateEntry(entry), data => CreatedAtAction(nameof(GetByMonth), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Entry entry)
    {
        return HandleAsync(() => _entryService.UpdateEntry(entry), _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        return HandleAsync(() => _entryService.DeleteEntry(id), _ => NoContent());
    }
}
