using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        var clientId = _authService.GetGoogleClientId();
        if (string.IsNullOrEmpty(clientId))
            return StatusCode(500, new { error = "Login com Google não está configurado." });

        var redirectUri = GetOAuthRedirectUri(providerLower);
        var scope = Uri.EscapeDataString("openid email profile");
        var authorizationUrl =
            "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            $"&scope={scope}" +
            "&access_type=offline" +
            "&prompt=select_account";

        return Redirect(authorizationUrl);
    }

    [AllowAnonymous]
    [HttpGet("oauth/{provider}/callback")]
    public async Task<IActionResult> HandleOAuthCallback(string provider, [FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? state)
    {
        var frontendLoginUrl = $"{_authService.GetFrontendBaseUrl()}/login?oauthError=1";

        try
        {
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                return Redirect(frontendLoginUrl);

            var clientId = _authService.GetGoogleClientId();
            var clientSecret = _authService.GetGoogleClientSecret();
            var redirectUri = GetOAuthRedirectUri(provider);

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
                return Redirect(frontendLoginUrl);

            var userInfoResponse = await client.GetAsync($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={tokenData.access_token}");
            var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>();

            if (userInfo == null)
                return Redirect(frontendLoginUrl);

            var result = await _authService.HandleOAuthCallbackAsync(provider, userInfo.id ?? "", userInfo.email ?? "", userInfo.name ?? "");
            return Redirect($"{_authService.GetFrontendBaseUrl()}/auth/callback?token={Uri.EscapeDataString(result.Token)}&refreshToken={Uri.EscapeDataString(result.RefreshToken)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OAuth Callback Error] {ex}");
            return Redirect(frontendLoginUrl);
        }
    }

    private string GetOAuthRedirectUri(string provider) => $"{_authService.GetApiBaseUrl()}/api/auth/oauth/{provider}/callback";

    [HttpPost("logout")]
    public Task<IActionResult> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        return HandleAsync(() => _authService.LogoutAsync(token), Ok(new { message = "Logout realizado com sucesso." }));
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { error = "Refresh token é obrigatório." });

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(result);
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}