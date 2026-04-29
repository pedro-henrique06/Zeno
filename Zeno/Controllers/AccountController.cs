using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Domain.Wallet;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : AppControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        return HandleAsync(() => _accountService.GetAccountsByUserAsync(userId), data => Ok(data));
    }

    [HttpGet("wallet/{walletId:guid}")]
    public Task<IActionResult> GetByWallet(Guid walletId)
    {
        var userId = GetUserId();
        return HandleAsync(() => _accountService.GetAccountsByWalletAsync(userId, walletId), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _accountService.GetAccountByIdAsync(userId, id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Account account)
    {
        var userId = GetUserId();
        return HandleAsync(() => _accountService.CreateAccountAsync(userId, account), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Account account)
    {
        var userId = GetUserId();
        return HandleAsync(() => _accountService.UpdateAccountAsync(userId, account), _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        return HandleAsync(() => _accountService.DeleteAccountAsync(userId, id), NoContent());
    }
}