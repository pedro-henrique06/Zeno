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
        return HandleAsync(() => _salaryService.GetSalariesByWallet(walletId), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        return HandleAsync(() => _salaryService.GetSalaryById(id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Salary salary)
    {
        return HandleAsync(() => _salaryService.CreateSalary(salary), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Salary salary)
    {
        return HandleAsync(() => _salaryService.UpdateSalary(salary), data => Ok(data));
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        return HandleAsync(() => _salaryService.DeleteSalary(id), data => Ok(data));
    }
}
