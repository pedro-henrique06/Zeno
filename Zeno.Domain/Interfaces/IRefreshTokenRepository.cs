using RefreshTokenEntity = Zeno.Domain.Auth.RefreshToken;

namespace Zeno.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenEntity?> GetByTokenAsync(string token);
    Task<RefreshTokenEntity?> GetByUserAndTokenAsync(Guid userId, string token);
    Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken);
    Task RevokeAsync(Guid userId, string token);
    Task RevokeAllUserTokensAsync(Guid userId);
}