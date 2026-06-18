using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses.Common;
using Zeno.Domain.CustomCategory;

namespace Zeno.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : AppControllerBase
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<IEnumerable<Category>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<Category>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var userId = GetUserId();
        var data = await _service.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<Category>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryRequest request)
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

[ApiController]
[Route("api/category-rules")]
public class CategoryRuleController : AppControllerBase
{
    private readonly ICategoryRuleService _service;

    public CategoryRuleController(ICategoryRuleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _service.GetAllAsync(userId);
        return Ok(ApiResponse<IEnumerable<CategoryRule>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _service.GetByIdAsync(userId, id);
        return result is not null ? Ok(ApiResponse<CategoryRule>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRuleRequest request)
    {
        var userId = GetUserId();
        var data = await _service.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<CategoryRule>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryRuleRequest request)
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