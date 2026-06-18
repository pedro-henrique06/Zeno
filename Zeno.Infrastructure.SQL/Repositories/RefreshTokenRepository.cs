using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using RefreshTokenEntity = Zeno.Domain.Auth.RefreshToken;

namespace Zeno.Infrastructure.SQL.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ZenoDbContext _context;

    public RefreshTokenRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshTokenEntity?> GetByTokenAsync(string token)
    {
        const string sql = @"SELECT id, userid, token, expiresat, revokedat, createdat
                             FROM refresh_tokens WHERE token = @Token";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Token = token });
        return row is null ? null : MapToRefreshToken(row);
    }

    public async Task<RefreshTokenEntity?> GetByUserAndTokenAsync(Guid userId, string token)
    {
        const string sql = @"SELECT id, userid, token, expiresat, revokedat, createdat
                             FROM refresh_tokens WHERE userid = @UserId AND token = @Token";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId, Token = token });
        return row is null ? null : MapToRefreshToken(row);
    }

    public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken)
    {
        const string sql = @"INSERT INTO refresh_tokens (id, userid, token, expiresat, revokedat, createdat)
                             VALUES (@Id, @UserId, @Token, @ExpiresAt, @RevokedAt, @CreatedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            refreshToken.Id,
            refreshToken.UserId,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            refreshToken.RevokedAt,
            refreshToken.CreatedAt
        });
        return refreshToken;
    }

    public async Task RevokeAsync(Guid userId, string token)
    {
        const string sql = @"UPDATE refresh_tokens SET revokedat = @RevokedAt
                             WHERE userid = @UserId AND token = @Token AND revokedat IS NULL";
        await _context.Connection.ExecuteAsync(sql, new { UserId = userId, RevokedAt = DateTime.UtcNow, Token = token });
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        const string sql = @"UPDATE refresh_tokens SET revokedat = @RevokedAt
                             WHERE userid = @UserId AND revokedat IS NULL";
        await _context.Connection.ExecuteAsync(sql, new { UserId = userId, RevokedAt = DateTime.UtcNow });
    }

    private static RefreshTokenEntity MapToRefreshToken(dynamic row)
    {
        return new RefreshTokenEntity
        {
            Id = row.id,
            UserId = row.userid,
            Token = row.token,
            ExpiresAt = row.expiresat,
            RevokedAt = row.revokedat,
            CreatedAt = row.createdat
        };
    }
}