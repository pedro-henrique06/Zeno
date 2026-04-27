using Microsoft.Extensions.Caching.Memory;
using Zeno.Application.Interfaces;

namespace Zeno.Application.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Revoke(string jti, TimeSpan? expiresIn = null)
    {
        _cache.Set(GetKey(jti), true, expiresIn ?? TimeSpan.FromHours(24));
    }

    public bool IsRevoked(string jti)
    {
        return _cache.TryGetValue(GetKey(jti), out _);
    }

    private static string GetKey(string jti) => $"revoked_token:{jti}";
}
