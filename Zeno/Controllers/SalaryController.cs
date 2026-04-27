using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Domain.Salary;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalaryController : AppControllerBase
{
    private readonly ISalaryService _salaryService;

    public SalaryController(ISalaryService salaryService)
    {
        _salaryService = salaryService;
    }

    [HttpGet("wallet/{walletId:guid}")]
    public Task<IActionResult> GetByWallet(Guid walletId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _salaryService.GetSalariesByWallet(userId, walletId), data => Ok(data));
    }

    [HttpGet("user/{userId:guid}")]
    public Task<IActionResult> GetByUser(Guid userId)
    {
        return HandleAsync(() => _salaryService.GetSalariesByUser(userId), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _salaryService.GetSalaryById(userId, id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Salary salary)
    {
        var userId = GetUserId();
        return HandleAsync(() => _salaryService.CreateSalary(userId, salary), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Salary salary)
    {
        var userId = GetUserId();
        return HandleAsync(() => _salaryService.UpdateSalary(userId, salary), data => Ok(data));
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _salaryService.DeleteSalary(userId, id), data => Ok(data));
    }
}
