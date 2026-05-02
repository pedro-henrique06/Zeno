using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;

namespace Zeno.Controllers;

public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? access_token { get; set; }
    [JsonPropertyName("token_type")]
    public string? token_type { get; set; }
    [JsonPropertyName("expires_in")]
    public int? expires_in { get; set; }
    [JsonPropertyName("refresh_token")]
    public string? refresh_token { get; set; }
    [JsonPropertyName("id_token")]
    public string? id_token { get; set; }
}

public class GoogleUserInfo
{
    [JsonPropertyName("id")]
    public string? id { get; set; }
    [JsonPropertyName("email")]
    public string? email { get; set; }
    [JsonPropertyName("name")]
    public string? name { get; set; }
    [JsonPropertyName("picture")]
    public string? picture { get; set; }
    [JsonPropertyName("verified_email")]
    public bool? verified_email { get; set; }
}

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
    public async Task<IActionResult> HandleOAuthCallback(string provider, [FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? state)
    {
        try
        {
            if (!string.IsNullOrEmpty(error))
                return BadRequest(new { error = $"OAuth error: {error}" });

            if (string.IsNullOrEmpty(code))
                return BadRequest(new { error = "Código de autorização não fornecido." });

            var clientId = _authService.GetGoogleClientId();
            var clientSecret = _authService.GetGoogleClientSecret();
            var redirectUri = $"https://zeno-production-51bb.up.railway.app/api/auth/oauth/{provider}/callback";

            var tokenEndpoint = "https://oauth2.googleapis.com/token";
            var tokenRequest = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            };

            using var client = new HttpClient();
            var tokenResponse = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();

            if (tokenData == null || string.IsNullOrEmpty(tokenData.access_token))
                return Unauthorized(new { error = "Falha ao obter token do Google." });

            var userInfoResponse = await client.GetAsync($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={tokenData.access_token}");
            var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>();

            if (userInfo == null)
                return Unauthorized(new { error = "Falha ao obter informações do usuário." });

            var result = await _authService.HandleOAuthCallbackAsync(provider, userInfo.id ?? "", userInfo.email ?? "", userInfo.name ?? "");
            return Redirect($"https://zeno-production-51bb.up.railway.app/auth/callback?token={result.Token}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OAuth Callback Error] {ex}");
            return StatusCode(500, new { error = "Erro interno no callback OAuth.", details = ex.Message });
        }
    }

    [HttpPost("logout")]
    public Task<IActionResult> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        return HandleAsync(() => _authService.LogoutAsync(token), Ok(new { message = "Logout realizado com sucesso." }));
    }
}