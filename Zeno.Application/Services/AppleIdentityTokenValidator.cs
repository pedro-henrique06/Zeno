using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;

namespace Zeno.Application.Services;

public class AppleIdentityTokenValidator : IAppleIdentityTokenValidator
{
    private const string Issuer = "https://appleid.apple.com";
    private const string KeysUrl = "https://appleid.apple.com/auth/keys";
    private const string KeysCacheKey = "apple-signin-keys";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly string[] _audiences;

    public AppleIdentityTokenValidator(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        // The app's bundle id. Expo Go signs with its own id (host.exp.Exponent), which only the
        // Development config adds: in production any Expo Go project could mint tokens for it.
        _audiences = (configuration["OAuth:Apple:ClientIds"] ?? "app.zeno.mobile")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task<AppleIdentity> ValidateAsync(string identityToken)
    {
        if (string.IsNullOrWhiteSpace(identityToken))
            throw Invalid();

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudiences = _audiences,
            IssuerSigningKeys = await GetKeysAsync(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
                .ValidateToken(identityToken, parameters, out _);
            var sub = principal.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(sub))
                throw Invalid();

            var email = principal.FindFirst("email")?.Value;
            var verified = principal.FindFirst("email_verified")?.Value is "true" or "True";
            return new AppleIdentity(sub, string.IsNullOrWhiteSpace(email) ? null : email, verified);
        }
        catch (SecurityTokenException)
        {
            throw Invalid();
        }
        catch (ArgumentException)
        {
            // Malformed token (not a JWT at all).
            throw Invalid();
        }
    }

    /// <summary>Apple rotates its signing keys rarely; a day of caching keeps logins off the network.</summary>
    private async Task<IList<SecurityKey>> GetKeysAsync()
    {
        if (_cache.TryGetValue(KeysCacheKey, out IList<SecurityKey>? cached) && cached is not null)
            return cached;

        var json = await _httpClient.GetStringAsync(KeysUrl);
        var keys = new JsonWebKeySet(json).GetSigningKeys();
        _cache.Set(KeysCacheKey, keys, TimeSpan.FromHours(24));
        return keys;
    }

    private static AppValidationException Invalid() =>
        new(new FluentValidation.Results.ValidationResult(
            new List<FluentValidation.Results.ValidationFailure>
            {
                new("IdentityToken", "Login com a Apple inválido ou expirado. Tente de novo.")
            }));
}
