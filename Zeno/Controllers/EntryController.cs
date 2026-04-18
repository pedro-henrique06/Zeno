using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Domain.Entry;
using Zeno.Responses;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntryController : ControllerBase
{
    private readonly IEntryService _entryService;

    public EntryController(IEntryService entryService)
    {
        _entryService = entryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByMonth([FromQuery] int? month, [FromQuery] int? year)
    {
        var query = new GetEntriesByMonthQuery { Month = month, Year = year };
        var result = await ExecuteAsync(async () => await _entryService.GetEntriesByMonth(query));

        return result.IsValid ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Entry entry)
    {
        var result = await ExecuteAsync(async () => await _entryService.CreateEntry(entry));

        return result.IsValid ? CreatedAtAction(nameof(GetByMonth), new { id = entry.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Entry entry)
    {
        var result = await ExecuteAsync(async () => await _entryService.UpdateEntry(entry));

        return result.IsValid ? NoContent() : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await ExecuteAsync(async () => await _entryService.DeleteEntry(id));

        return result.IsValid ? NoContent() : BadRequest(result.Errors);
    }

    private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var data = await action();
            return ServiceResult<T>.Ok(data);
        }
        catch (AppValidationException ex)
        {
            var errors = ex.ValidationResult.Errors.Select(e => new ValidationError
            {
                Property = e.PropertyName,
                Error = e.ErrorMessage
            });

            return ServiceResult<T>.Fail(errors);
        }
    }
}
