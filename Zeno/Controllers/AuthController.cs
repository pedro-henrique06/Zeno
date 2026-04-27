using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;

namespace Zeno.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : AppControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        return HandleAsync(() => _authService.LoginAsync(request), data => Ok(data));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        return HandleAsync(() => _authService.RegisterAsync(request), data => CreatedAtAction(nameof(Login), data));
    }

    [HttpPost("logout")]
    public Task<IActionResult> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        return HandleAsync(() => _authService.LogoutAsync(token), Ok(new { message = "Logout realizado com sucesso." }));
    }
}
