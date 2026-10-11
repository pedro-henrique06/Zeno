using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Zeno.Application.Interfaces;

namespace Zeno.Application.Services;

public class AppleTokenService : IAppleTokenService
{
    private const string Audience = "https://appleid.apple.com";
    private const string TokenUrl = "https://appleid.apple.com/auth/token";
    private const string RevokeUrl = "https://appleid.apple.com/auth/revoke";

    private readonly HttpClient _httpClient;
    private readonly Func<DateTime> _now;
    private readonly string? _teamId;
    private readonly string? _keyId;
    private readonly string? _privateKey;
    private readonly string _clientId;

    public AppleTokenService(HttpClient httpClient, IConfiguration configuration, IClock clock)
    {
        _httpClient = httpClient;
        _now = () => clock.UtcNow;
        _teamId = configuration["OAuth:Apple:TeamId"];
        _keyId = configuration["OAuth:Apple:KeyId"];
        // Env vars often carry the .p8 on one line with literal "\n"; put the newlines back.
        _privateKey = configuration["OAuth:Apple:PrivateKey"]?.Replace("\\n", "\n");
        // The native app signs in with its bundle id, which is also the client_id for these calls.
        _clientId = configuration["OAuth:Apple:BundleId"] is { Length: > 0 } id ? id : "app.zeno.mobile";
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_teamId) && !string.IsNullOrWhiteSpace(_keyId) && !string.IsNullOrWhiteSpace(_privateKey);

    public async Task<string?> ExchangeCodeAsync(string authorizationCode)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(authorizationCode)) return null;

        var response = await PostAsync(TokenUrl, new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = CreateClientSecret(),
            ["code"] = authorizationCode,
            ["grant_type"] = "authorization_code",
        });
        if (response is null) return null;

        using var json = JsonDocument.Parse(response);
        return json.RootElement.TryGetProperty("refresh_token", out var token) ? token.GetString() : null;
    }

    public async Task<bool> RevokeAsync(string refreshToken)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(refreshToken)) return false;

        var response = await PostAsync(RevokeUrl, new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = CreateClientSecret(),
            ["token"] = refreshToken,
            ["token_type_hint"] = "refresh_token",
        });
        return response is not null;
    }

    /// <summary>The ES256 JWT Apple wants as client_secret, signed with the .p8 key. Short-lived on purpose.</summary>
    public string CreateClientSecret()
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(_privateKey);
        var key = new ECDsaSecurityKey(ecdsa) { KeyId = _keyId };
        var now = _now();
        var token = new JwtSecurityToken(
            issuer: _teamId,
            audience: Audience,
            claims: new[] { new Claim("sub", _clientId) },
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256) { CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } });
        token.Payload["iat"] = new DateTimeOffset(now).ToUnixTimeSeconds();
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string?> PostAsync(string url, Dictionary<string, string> form)
    {
        try
        {
            using var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(form));
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) return body;
            Console.WriteLine($"[Apple] {url} returned {(int)response.StatusCode}: {body}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[Apple] {url} unreachable: {ex.Message}");
            return null;
        }
    }
}
