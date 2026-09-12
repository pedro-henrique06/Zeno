using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Notification;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly ZenoMongoContext _context;

    public NotificationPreferenceRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<NotificationPreference?> GetByUserAsync(Guid userId)
    {
        return await _context.NotificationPreferences
            .Find(x => x.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<NotificationPreference> UpsertAsync(NotificationPreference preference)
    {
        var filter = Builders<NotificationPreference>.Filter.Eq(x => x.UserId, preference.UserId);
        var update = Builders<NotificationPreference>.Update
            .SetOnInsert(x => x.CreatedAt, preference.CreatedAt)
            .Set(x => x.DailyEnabled, preference.DailyEnabled)
            .Set(x => x.SendHour, preference.SendHour)
            .Set(x => x.TimeZoneId, preference.TimeZoneId)
            .Set(x => x.UpdatedAt, preference.UpdatedAt);

        var options = new FindOneAndUpdateOptions<NotificationPreference>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        return await _context.NotificationPreferences.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<IEnumerable<NotificationPreference>> GetAllEnabledAsync()
    {
        return await _context.NotificationPreferences
            .Find(x => x.DailyEnabled)
            .ToListAsync();
    }

    public async Task MarkSentAsync(Guid userId, DateOnly localDate)
    {
        var update = Builders<NotificationPreference>.Update.Set(x => x.LastSentOn, localDate);
        await _context.NotificationPreferences.UpdateOneAsync(x => x.UserId == userId, update);
    }
}
