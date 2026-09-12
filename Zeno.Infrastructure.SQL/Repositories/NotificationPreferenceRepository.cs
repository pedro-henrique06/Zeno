using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private const string Columns = "userid, dailyenabled, sendhour, timezoneid, lastsenton, createdat, updatedat";

    private readonly ZenoDbContext _context;

    public NotificationPreferenceRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationPreference?> GetByUserAsync(Guid userId)
    {
        const string sql = $@"SELECT {Columns} FROM notificationpreferences WHERE userid = @UserId";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId });
        return row is null ? null : Map(row);
    }

    public async Task<NotificationPreference> UpsertAsync(NotificationPreference preference)
    {
        const string sql = $@"INSERT INTO notificationpreferences (userid, dailyenabled, sendhour, timezoneid, lastsenton, createdat, updatedat)
                              VALUES (@UserId, @DailyEnabled, @SendHour, @TimeZoneId, @LastSentOn, @CreatedAt, @UpdatedAt)
                              ON CONFLICT (userid) DO UPDATE
                                  SET dailyenabled = EXCLUDED.dailyenabled,
                                      sendhour = EXCLUDED.sendhour,
                                      timezoneid = EXCLUDED.timezoneid,
                                      updatedat = EXCLUDED.updatedat
                              RETURNING {Columns}";

        var row = await _context.Connection.QueryFirstAsync<dynamic>(sql, new
        {
            preference.UserId,
            preference.DailyEnabled,
            preference.SendHour,
            preference.TimeZoneId,
            preference.LastSentOn,
            preference.CreatedAt,
            preference.UpdatedAt
        });

        return Map(row);
    }

    public async Task<IEnumerable<NotificationPreference>> GetAllEnabledAsync()
    {
        const string sql = $@"SELECT {Columns} FROM notificationpreferences WHERE dailyenabled = true";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql);
        return rows.Select(r => Map(r)).Cast<NotificationPreference>();
    }

    public async Task MarkSentAsync(Guid userId, DateOnly localDate)
    {
        const string sql = @"UPDATE notificationpreferences SET lastsenton = @LocalDate WHERE userid = @UserId";
        await _context.Connection.ExecuteAsync(sql, new { UserId = userId, LocalDate = localDate });
    }

    private static NotificationPreference Map(dynamic row)
    {
        return new NotificationPreference
        {
            UserId = row.userid,
            DailyEnabled = row.dailyenabled,
            SendHour = row.sendhour,
            TimeZoneId = row.timezoneid,
            LastSentOn = row.lastsenton is null ? null : (DateOnly)row.lastsenton,
            CreatedAt = row.createdat,
            UpdatedAt = row.updatedat
        };
    }
}
