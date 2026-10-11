using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : AppControllerBase
{
    private readonly IUserService _userService;
    private readonly IAccountDeletionService _accountDeletionService;
    private readonly IAuthService _authService;

    public UserController(IUserService userService, IAccountDeletionService accountDeletionService, IAuthService authService)
    {
        _userService = userService;
        _accountDeletionService = accountDeletionService;
        _authService = authService;
    }

    [HttpGet("me")]
    public Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        return HandleAsync(() => _userService.GetProfile(userId), data => Ok(data));
    }

    /// <summary>Permanently deletes the signed-in user's account and data, then revokes this access token.</summary>
    [HttpDelete("me")]
    public Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        return HandleAsync(async () =>
        {
            await _accountDeletionService.DeleteAccountAsync(userId);
            await _authService.LogoutAsync(token);
        }, NoContent());
    }

    [HttpPut("me")]
    public Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _userService.UpdateProfile(userId, request), data => Ok(data));
    }

    [HttpPut("me/password")]
    public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _userService.ChangePassword(userId, request), NoContent());
    }

    [HttpPut("me/daily-budget")]
    public Task<IActionResult> UpdateDailyBudget([FromBody] UpdateDailyBudgetRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _userService.UpdateDailyBudget(userId, request), data => Ok(data));
    }

    [HttpPut("me/currency")]
    public Task<IActionResult> UpdateCurrency([FromBody] UpdateCurrencyRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _userService.UpdateCurrency(userId, request), data => Ok(data));
    }

    [HttpPut("me/language")]
    public Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        var userId = GetUserId();
        return HandleAsync(() => _userService.UpdateLanguage(userId, request), data => Ok(data));
    }
}
