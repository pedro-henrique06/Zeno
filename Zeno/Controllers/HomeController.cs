using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Domain.Home;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : AppControllerBase
{
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll()
    {
        return HandleAsync(() => _homeService.GetAllHomes(), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        return HandleAsync(() => _homeService.GetHomeById(id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Home home)
    {
        return HandleAsync(() => _homeService.CreateHome(home), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Home home)
    {
        return HandleAsync(() => _homeService.UpdateHome(home), _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        return HandleAsync(() => _homeService.DeleteHome(id), _ => NoContent());
    }

    [HttpPost("{homeId:guid}/wallets/{walletId:guid}")]
    public Task<IActionResult> AddWallet(Guid homeId, Guid walletId)
    {
        return HandleAsync(() => _homeService.AddWalletToHome(homeId, walletId), NoContent());
    }

    [HttpDelete("{homeId:guid}/wallets/{walletId:guid}")]
    public Task<IActionResult> RemoveWallet(Guid homeId, Guid walletId)
    {
        return HandleAsync(() => _homeService.RemoveWalletFromHome(homeId, walletId), NoContent());
    }

    [HttpPost("{homeId:guid}/expenses")]
    public Task<IActionResult> CreateExpense(Guid homeId, [FromBody] HomeExpense expense)
    {
        expense.HomeId = homeId;
        return HandleAsync(() => _homeService.CreateExpense(expense), data => Ok(data));
    }

    [HttpGet("{homeId:guid}/expenses")]
    public Task<IActionResult> GetExpensesByMonth(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        return HandleAsync(() => _homeService.GetExpensesByMonth(homeId, month, year), data => Ok(data));
    }

    [HttpDelete("expenses/{expenseId:guid}")]
    public Task<IActionResult> DeleteExpense(Guid expenseId)
    {
        return HandleAsync(() => _homeService.DeleteExpense(expenseId), NoContent());
    }

    [HttpGet("{homeId:guid}/split")]
    public Task<IActionResult> CalculateSplit(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        return HandleAsync(() => _homeService.CalculateExpenseSplit(homeId, month, year), data => Ok(data));
    }
}
