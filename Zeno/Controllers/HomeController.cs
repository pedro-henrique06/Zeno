using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Homes;
using Zeno.Application.Responses.Common;
using Zeno.Domain.Home;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : AppControllerBase
{
    private readonly IHomeService _homeService;
    private readonly IHomeMemberService _memberService;
    private readonly IHomeExpenseService _expenseService;
    private readonly IHomeSplitService _splitService;
    private readonly IHomeBudgetService _budgetService;

    public HomeController(
        IHomeService homeService,
        IHomeMemberService memberService,
        IHomeExpenseService expenseService,
        IHomeSplitService splitService,
        IHomeBudgetService budgetService)
    {
        _homeService = homeService;
        _memberService = memberService;
        _expenseService = expenseService;
        _splitService = splitService;
        _budgetService = budgetService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var result = await _homeService.GetAllHomes(userId);
        return Ok(ApiResponse<IEnumerable<Home>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _homeService.GetHomeById(userId, id);
        return result is not null ? Ok(ApiResponse<Home>.Ok(result)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHomeRequest request)
    {
        var userId = GetUserId();
        var data = await _homeService.CreateHome(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<Home>.Ok(data));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateHomeRequest request)
    {
        var userId = GetUserId();
        await _homeService.UpdateHome(userId, request);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        await _homeService.DeleteHome(userId, id);
        return NoContent();
    }

    [HttpGet("{homeId:guid}/wallets")]
    public async Task<IActionResult> GetWallets(Guid homeId)
    {
        var userId = GetUserId();
        var result = await _splitService.GetWallets(userId, homeId);
        return Ok(ApiResponse<IEnumerable<HomeWallet>>.Ok(result));
    }

    [HttpPost("{homeId:guid}/wallets/{walletId:guid}")]
    public async Task<IActionResult> AddWallet(Guid homeId, Guid walletId)
    {
        var userId = GetUserId();
        await _splitService.AddWalletToHome(userId, homeId, walletId);
        return NoContent();
    }

    [HttpDelete("{homeId:guid}/wallets/{walletId:guid}")]
    public async Task<IActionResult> RemoveWallet(Guid homeId, Guid walletId)
    {
        var userId = GetUserId();
        await _splitService.RemoveWalletFromHome(userId, homeId, walletId);
        return NoContent();
    }

    [HttpPost("{homeId:guid}/expenses")]
    public async Task<IActionResult> CreateExpense(Guid homeId, [FromBody] CreateHomeExpenseRequest request)
    {
        var userId = GetUserId();
        request.HomeId = homeId;
        var data = await _expenseService.CreateExpense(userId, request);
        return Ok(ApiResponse<HomeExpense>.Ok(data));
    }

    [HttpGet("{homeId:guid}/expenses")]
    public async Task<IActionResult> GetExpensesByMonth(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        var result = await _expenseService.GetExpensesByMonth(userId, homeId, month, year);
        return Ok(ApiResponse<IEnumerable<HomeExpense>>.Ok(result));
    }

    [HttpDelete("{homeId:guid}/expenses/{expenseId:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid homeId, Guid expenseId)
    {
        var userId = GetUserId();
        await _expenseService.DeleteExpense(userId, homeId, expenseId);
        return NoContent();
    }

    [HttpGet("{homeId:guid}/split")]
    public async Task<IActionResult> CalculateSplit(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        var result = await _splitService.CalculateExpenseSplit(userId, homeId, month, year);
        return Ok(ApiResponse<IEnumerable<ExpenseSplitResult>>.Ok(result));
    }

    [HttpPost("{homeId:guid}/members")]
    public async Task<IActionResult> InviteMember(Guid homeId, [FromBody] AddHomeMemberRequest request)
    {
        var userId = GetUserId();
        await _memberService.InviteMember(userId, homeId, request);
        return Ok(ApiResponse<object>.Ok(new { message = "Membro adicionado com sucesso." }));
    }

    [HttpDelete("{homeId:guid}/members/{memberUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid homeId, Guid memberUserId)
    {
        var userId = GetUserId();
        await _memberService.RemoveMember(userId, homeId, memberUserId);
        return Ok(ApiResponse<object>.Ok(new { message = "Membro removido com sucesso." }));
    }

    [HttpGet("{homeId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid homeId)
    {
        var userId = GetUserId();
        var result = await _memberService.GetMembers(userId, homeId);
        return Ok(ApiResponse<IEnumerable<HomeMember>>.Ok(result));
    }

    [HttpGet("{homeId:guid}/budget-alert")]
    public async Task<IActionResult> GetBudgetAlert(Guid homeId, [FromQuery] int month, [FromQuery] int year)
    {
        var userId = GetUserId();
        var result = await _budgetService.GetBudgetAlertAsync(userId, homeId, month, year);
        return Ok(ApiResponse<object>.Ok(result));
    }
}