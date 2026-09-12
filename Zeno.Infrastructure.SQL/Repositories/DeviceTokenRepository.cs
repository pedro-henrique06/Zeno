using Dapper;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class DeviceTokenRepository : IDeviceTokenRepository
{
    private const string Columns = "id, userid, token, platform, isactive, createdat, lastseenat";

    private readonly ZenoDbContext _context;

    public DeviceTokenRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeviceToken>> GetActiveByUserAsync(Guid userId)
    {
        const string sql = $@"SELECT {Columns}
                              FROM devicetokens
                              WHERE userid = @UserId AND isactive = true
                              ORDER BY lastseenat DESC";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => Map(r)).Cast<DeviceToken>();
    }

    public async Task<DeviceToken?> GetByTokenAsync(string token)
    {
        const string sql = $@"SELECT {Columns} FROM devicetokens WHERE token = @Token";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Token = token });
        return row is null ? null : Map(row);
    }

    public async Task<DeviceToken> UpsertAsync(DeviceToken deviceToken)
    {
        // O mesmo aparelho pode trocar de usuario (logout/login), por isso o conflito reatribui o token.
        const string sql = $@"INSERT INTO devicetokens (id, userid, token, platform, isactive, createdat, lastseenat)
                              VALUES (@Id, @UserId, @Token, @Platform, @IsActive, @CreatedAt, @LastSeenAt)
                              ON CONFLICT (token) DO UPDATE
                                  SET userid = EXCLUDED.userid,
                                      platform = EXCLUDED.platform,
                                      isactive = true,
                                      lastseenat = EXCLUDED.lastseenat
                              RETURNING {Columns}";

        var row = await _context.Connection.QueryFirstAsync<dynamic>(sql, new
        {
            deviceToken.Id,
            deviceToken.UserId,
            deviceToken.Token,
            Platform = (int)deviceToken.Platform,
            deviceToken.IsActive,
            deviceToken.CreatedAt,
            deviceToken.LastSeenAt
        });

        return Map(row);
    }

    public async Task DeactivateAsync(string token)
    {
        const string sql = @"UPDATE devicetokens SET isactive = false WHERE token = @Token";
        await _context.Connection.ExecuteAsync(sql, new { Token = token });
    }

    public async Task DeleteByUserAndTokenAsync(Guid userId, string token)
    {
        const string sql = @"DELETE FROM devicetokens WHERE userid = @UserId AND token = @Token";
        await _context.Connection.ExecuteAsync(sql, new { UserId = userId, Token = token });
    }

    private static DeviceToken Map(dynamic row)
    {
        return new DeviceToken
        {
            Id = row.id,
            UserId = row.userid,
            Token = row.token,
            Platform = (DevicePlatform)(int)row.platform,
            IsActive = row.isactive,
            CreatedAt = row.createdat,
            LastSeenAt = row.lastseenat
        };
    }
}
