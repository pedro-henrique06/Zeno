using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

    [AllowAnonymous]
    [HttpGet("oauth/{provider}")]
    public IActionResult InitiateOAuthLogin(string provider)
    {
        var providerLower = provider.ToLower();
        if (providerLower != "google")
            return BadRequest(new { error = "Provedor OAuth não suportado." });

        var redirectUrl = $"https://zeno-production-51bb.up.railway.app/api/auth/oauth/{providerLower}/callback";
        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            IsPersistent = true
        };

        return Challenge(properties, "Google");
    }

    [AllowAnonymous]
    [HttpGet("oauth/{provider}/callback")]
    public async Task<IActionResult> HandleOAuthCallback(string provider, [FromQuery] string? code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
            return BadRequest(new { error = $"OAuth error: {error}" });

        if (string.IsNullOrEmpty(code))
            return BadRequest(new { error = "Código de autorização não fornecido." });

        var auth = await HttpContext.AuthenticateAsync("Google");
        if (!auth.Succeeded)
            return Unauthorized(new { error = "Falha na autenticação Google." });

        var externalUser = auth.Principal;
        var providerId = externalUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var email = externalUser?.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var name = externalUser?.FindFirst(ClaimTypes.Name)?.Value ?? "";

        var result = await _authService.HandleOAuthCallbackAsync(provider, providerId, email, name);
        return Redirect($"https://{Request.Host}/auth/callback?token={result.Token}");
    }

    [HttpPost("logout")]
    public Task<IActionResult> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        return HandleAsync(() => _authService.LogoutAsync(token), Ok(new { message = "Logout realizado com sucesso." }));
    }
}