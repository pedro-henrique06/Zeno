using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Domain.Wallet;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : AppControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll()
    {
        return HandleAsync(() => _walletService.GetAllWallets(), data => Ok(data));
    }

    [HttpGet("user/{userId:guid}")]
    public Task<IActionResult> GetByUser(Guid userId)
    {
        return HandleAsync(() => _walletService.GetWalletsByUser(userId), data => Ok(data));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
    {
        return HandleAsync(() => _walletService.GetWalletById(id), data => data is not null ? Ok(data) : NotFound());
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] Wallet wallet)
    {
        return HandleAsync(() => _walletService.CreateWallet(wallet), data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut]
    public Task<IActionResult> Update([FromBody] Wallet wallet)
    {
        return HandleAsync(() => _walletService.UpdateWallet(wallet), _ => NoContent());
    }

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
    {
        return HandleAsync(() => _walletService.DeleteWallet(id), _ => NoContent());
    }
}
