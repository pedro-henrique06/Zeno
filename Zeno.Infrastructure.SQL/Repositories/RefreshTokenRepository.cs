using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using RefreshTokenEntity = Zeno.Domain.Auth.RefreshToken;

namespace Zeno.Infrastructure.SQL.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ZenoMongoContext _context;

    public RefreshTokenRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<RefreshTokenEntity?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens.Find(x => x.Token == token).FirstOrDefaultAsync();
    }

    public async Task<RefreshTokenEntity?> GetByUserAndTokenAsync(Guid userId, string token)
    {
        return await _context.RefreshTokens
            .Find(x => x.UserId == userId && x.Token == token)
            .FirstOrDefaultAsync();
    }

    public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken)
    {
        await _context.RefreshTokens.InsertOneAsync(refreshToken);
        return refreshToken;
    }

    public async Task RevokeAsync(Guid userId, string token)
    {
        var filter = Builders<RefreshTokenEntity>.Filter.Eq(x => x.UserId, userId) &
                     Builders<RefreshTokenEntity>.Filter.Eq(x => x.Token, token) &
                     Builders<RefreshTokenEntity>.Filter.Eq(x => x.RevokedAt, null);

        var update = Builders<RefreshTokenEntity>.Update
            .Set(x => x.RevokedAt, DateTime.UtcNow);

        await _context.RefreshTokens.UpdateOneAsync(filter, update);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var filter = Builders<RefreshTokenEntity>.Filter.Eq(x => x.UserId, userId) &
                     Builders<RefreshTokenEntity>.Filter.Eq(x => x.RevokedAt, null);

        var update = Builders<RefreshTokenEntity>.Update
            .Set(x => x.RevokedAt, DateTime.UtcNow);

        await _context.RefreshTokens.UpdateManyAsync(filter, update);
    }
}
