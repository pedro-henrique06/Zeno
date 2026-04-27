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
        var userId = GetUserId();
        return HandleAsync(() => _homeService.GetAllHomes(userId), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.GetHomeById(userId, id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Home home)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.CreateHome(userId, home), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Home home)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.UpdateHome(userId, home), _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.DeleteHome(userId, id), _ => NoContent());
    }

    [HttpGet("{homeId:guid}/wallets")]
    public Task<IActionResult> GetWallets(Guid homeId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.GetWallets(userId, homeId), data => Ok(data));
    }

    [HttpPost("{homeId:guid}/wallets/{walletId:guid}")]
    public Task<IActionResult> AddWallet(Guid homeId, Guid walletId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.AddWalletToHome(userId, homeId, walletId), NoContent());
    }

    [HttpDelete("{homeId:guid}/wallets/{walletId:guid}")]
    public Task<IActionResult> RemoveWallet(Guid homeId, Guid walletId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.RemoveWalletFromHome(userId, homeId, walletId), NoContent());
    }

    [HttpPost("{homeId:guid}/expenses")]
    public Task<IActionResult> CreateExpense(Guid homeId, [FromBody] HomeExpense expense)
    {
        var userId = GetUserId();
        expense.HomeId = homeId;
        return HandleAsync(() => _homeService.CreateExpense(userId, expense), data => Ok(data));
    }

    [HttpGet("{homeId:guid}/expenses")]
    public Task<IActionResult> GetExpensesByMonth(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.GetExpensesByMonth(userId, homeId, month, year), data => Ok(data));
    }

    [HttpDelete("expenses/{expenseId:guid}")]
    public Task<IActionResult> DeleteExpense(Guid expenseId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.DeleteExpense(userId, expenseId), NoContent());
    }

    [HttpGet("{homeId:guid}/split")]
    public Task<IActionResult> CalculateSplit(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.CalculateExpenseSplit(userId, homeId, month, year), data => Ok(data));
    }

    [HttpPost("{homeId:guid}/members/{memberUserId:guid}")]
    public Task<IActionResult> InviteMember(Guid homeId, Guid memberUserId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.InviteMember(userId, homeId, memberUserId), Ok(new { message = "Membro adicionado com sucesso." }));
    }

    [HttpDelete("{homeId:guid}/members/{memberUserId:guid}")]
    public Task<IActionResult> RemoveMember(Guid homeId, Guid memberUserId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.RemoveMember(userId, homeId, memberUserId), Ok(new { message = "Membro removido com sucesso." }));
    }

    [HttpGet("{homeId:guid}/members")]
    public Task<IActionResult> GetMembers(Guid homeId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.GetMembers(userId, homeId), data => Ok(data));
    }

    [HttpGet("{homeId:guid}/budget-alert")]
    public Task<IActionResult> GetBudgetAlert(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _homeService.GetBudgetAlertAsync(userId, homeId, month, year), data => Ok(data));
    }
}
