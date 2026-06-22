using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.MonthlyExpenseCategories;
using Zeno.Application.Responses.Common;
using MonthlyExpenseCategoryEntity = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;

namespace Zeno.Controllers;

[ApiController]
[Route("api/monthly-expense-categories")]
public class MonthlyExpenseCategoryController : AppControllerBase
{
    private readonly IMonthlyExpenseCategoryService _service;

    public MonthlyExpenseCategoryController(IMonthlyExpenseCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<IEnumerable<MonthlyExpenseCategoryEntity>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<MonthlyExpenseCategoryEntity>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMonthlyExpenseCategoryRequest request)
    {
        var userId = GetUserId();
        return await HandleAsync(
            () => _service.CreateAsync(userId, request),
            data => CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<MonthlyExpenseCategoryEntity>.Ok(data)));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMonthlyExpenseCategoryRequest request)
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
