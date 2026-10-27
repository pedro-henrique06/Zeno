using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Zeno.Application.Services;
using Zeno.Domain.Notification;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Tests;

public class NotificationPreferenceMappingTests
{
    static NotificationPreferenceMappingTests()
    {
        // Runs the context's static constructor, which registers the class map. MongoClient does not connect here.
        _ = new ZenoMongoContext("mongodb://localhost:27017", new AesEncryptionService("test-key-with-enough-entropy-1234567890"));
    }

    [Fact]
    public void Reads_DocumentWithTheObjectIdThatUpsertAdds()
    {
        var userId = Guid.NewGuid();
        var document = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "UserId", new BsonBinaryData(userId, GuidRepresentation.Standard) },
            { "DailyEnabled", true },
            { "SendHour", 20 },
            { "TimeZoneId", "America/Sao_Paulo" },
            { "CreatedAt", DateTime.UtcNow },
            { "UpdatedAt", DateTime.UtcNow },
        };

        var preference = BsonSerializer.Deserialize<NotificationPreference>(document);

        Assert.Equal(userId, preference.UserId);
        Assert.True(preference.DailyEnabled);
        Assert.Equal(20, preference.SendHour);
    }
}
