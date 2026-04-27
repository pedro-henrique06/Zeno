namespace Zeno.Application.Interfaces;

public interface ITokenBlacklistService
{
    void Revoke(string jti, TimeSpan? expiresIn = null);
    bool IsRevoked(string jti);
}
