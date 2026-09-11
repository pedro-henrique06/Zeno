using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Houses;
using Zeno.Application.Responses.Common;
using Zeno.Domain.House;

namespace Zeno.Controllers;

[ApiController]
[Route("api/houses")]
public class HouseController : AppControllerBase
{
    private readonly IHouseService _service;

    public HouseController(IHouseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<IEnumerable<House>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<House>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHouseRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.CreateAsync(userId, request),
            data => CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<House>.Ok(data)));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateHouseRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.UpdateAsync(userId, request),
            NoContent());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.DeleteAsync(userId, id),
            NoContent());
    }
}
