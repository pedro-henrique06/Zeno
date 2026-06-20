using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Domain.Wallet;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : AppControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IBalanceService _balanceService;

    public WalletController(IWalletService walletService, IBalanceService balanceService)
    {
        _walletService = walletService;
        _balanceService = balanceService;
    }

    [HttpGet("balances")]
    public Task<IActionResult> GetAggregatedBalances([FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.GetAggregatedDailyBalancesAsync(userId, month, year), data => Ok(data));
    }

    [HttpGet("{id:guid}/balances")]
    public Task<IActionResult> GetBalances(Guid id, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.GetDailyBalancesAsync(userId, id, month, year), data => Ok(data));
    }

    [HttpGet("{id:guid}/daily-average")]
    public Task<IActionResult> GetDailyAverage(Guid id, [FromQuery] int months = 3)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.GetDailyAverageAsync(userId, id, months), data => Ok(data));
    }

    [HttpGet("{id:guid}/forecast")]
    public Task<IActionResult> GetForecast(Guid id, [FromQuery] int months = 3)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.GetForecastAsync(userId, id, months), data => Ok(data));
    }

    [HttpGet("{id:guid}/card-invoice")]
    public Task<IActionResult> GetCardInvoice(Guid id, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.GetCardInvoiceAsync(userId, id, month, year), data => Ok(data));
    }

    [HttpGet("{id:guid}/daily-forecast")]
    public Task<IActionResult> GetDailyForecast(Guid id, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.GetDailyForecastAsync(userId, id, month, year), data => Ok(data));
    }

    [HttpPut("{id:guid}/budget")]
    public Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateWalletBudgetRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _balanceService.UpdateBudgetAsync(userId, id, request), data => Ok(data));
    }

    [HttpGet]
    public Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        return HandleAsync(() => _walletService.GetAllWallets(userId), data => Ok(data));
    }

    [HttpGet("user/{userId:guid}")]
    public Task<IActionResult> GetByUser(Guid userId)
    {
        return HandleAsync(() => _walletService.GetWalletsByUser(userId), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _walletService.GetWalletById(userId, id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Wallet wallet)
    {
        var userId = GetUserId();
        return HandleAsync(() => _walletService.CreateWallet(userId, wallet), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Wallet wallet)
    {
        var userId = GetUserId();
        return HandleAsync(() => _walletService.UpdateWallet(userId, wallet), _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _walletService.DeleteWallet(userId, id), _ => NoContent());
    }
}
