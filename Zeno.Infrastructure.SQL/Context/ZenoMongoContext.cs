using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Zeno.Application.Interfaces;
using Zeno.Domain.User;
using Zeno.Domain.Entry;
using Zeno.Domain.Auth;
using Zeno.Domain.Enum;
using Zeno.Domain.Notification;
using Zeno.Infrastructure.SQL.Serialization;
using Tag = Zeno.Domain.Tag.Tag;
using House = Zeno.Domain.House.House;
using HouseMember = Zeno.Domain.House.HouseMember;
using MonthlyExpenseCategory = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;
using PushSubscription = Zeno.Domain.Push.PushSubscription;

namespace Zeno.Infrastructure.SQL.Context;

public class ZenoMongoContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;

    static ZenoMongoContext()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(typeof(Currency), new EnumSerializer<Currency>(BsonType.String));
        BsonSerializer.RegisterSerializer(typeof(Language), new EnumSerializer<Language>(BsonType.String));
    }

    public ZenoMongoContext(string connectionString, IEncryptionService encryptionService, string databaseName = "zeno_db")
    {
        RegisterEncryptedClassMaps(encryptionService);
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);
    }

    private static void RegisterEncryptedClassMaps(IEncryptionService encryptionService)
    {
        var encryptedDecimal = new EncryptedDecimalSerializer(encryptionService);
        var encryptedString = new EncryptedStringSerializer(encryptionService);

        if (!BsonClassMap.IsClassMapRegistered(typeof(Entry)))
        {
            BsonClassMap.RegisterClassMap<Entry>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(e => e.Value).SetSerializer(encryptedDecimal);
                cm.GetMemberMap(e => e.Title).SetSerializer(encryptedString);
                cm.GetMemberMap(e => e.Description).SetSerializer(encryptedString);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(u => u.DailyBudget).SetSerializer(new NullableSerializer<decimal>(encryptedDecimal));
                cm.GetMemberMap(u => u.Name).SetSerializer(encryptedString);
                cm.GetMemberMap(u => u.Phone).SetSerializer(encryptedString);
                cm.GetMemberMap(u => u.Document).SetSerializer(encryptedString);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(House)))
        {
            BsonClassMap.RegisterClassMap<House>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(h => h.Name).SetSerializer(encryptedString);
                cm.GetMemberMap(h => h.Description).SetSerializer(encryptedString);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(HouseMember)))
        {
            BsonClassMap.RegisterClassMap<HouseMember>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(m => m.Name).SetSerializer(encryptedString);
                cm.GetMemberMap(m => m.Email).SetSerializer(encryptedString);
            });
        }
    }

    public async Task CreateIndexesAsync()
    {
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true }));
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Provider).Ascending(x => x.ProviderId)));

        await Entries.Indexes.CreateOneAsync(new CreateIndexModel<Entry>(
            Builders<Entry>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.Date)));

        await RefreshTokens.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(x => x.Token),
            new CreateIndexOptions { Unique = true }));
        await RefreshTokens.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(x => x.UserId)));

        await Tags.Indexes.CreateOneAsync(new CreateIndexModel<Tag>(
            Builders<Tag>.IndexKeys.Ascending(x => x.UserId)));

        await MonthlyExpenseCategories.Indexes.CreateOneAsync(new CreateIndexModel<MonthlyExpenseCategory>(
            Builders<MonthlyExpenseCategory>.IndexKeys.Ascending(x => x.UserId)));

        await PushSubscriptions.Indexes.CreateOneAsync(new CreateIndexModel<PushSubscription>(
            Builders<PushSubscription>.IndexKeys.Ascending(x => x.Endpoint),
            new CreateIndexOptions { Unique = true }));
        await PushSubscriptions.Indexes.CreateOneAsync(new CreateIndexModel<PushSubscription>(
            Builders<PushSubscription>.IndexKeys.Ascending(x => x.UserId)));

        await Houses.Indexes.CreateOneAsync(new CreateIndexModel<House>(
            Builders<House>.IndexKeys.Ascending(x => x.UserId)));

        await DeviceTokens.Indexes.CreateOneAsync(new CreateIndexModel<DeviceToken>(
            Builders<DeviceToken>.IndexKeys.Ascending(x => x.Token),
            new CreateIndexOptions { Unique = true }));
        await DeviceTokens.Indexes.CreateOneAsync(new CreateIndexModel<DeviceToken>(
            Builders<DeviceToken>.IndexKeys.Ascending(x => x.UserId)));

        await NotificationPreferences.Indexes.CreateOneAsync(new CreateIndexModel<NotificationPreference>(
            Builders<NotificationPreference>.IndexKeys.Ascending(x => x.UserId),
            new CreateIndexOptions { Unique = true }));
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Entry> Entries => _database.GetCollection<Entry>("entries");
    public IMongoCollection<Tag> Tags => _database.GetCollection<Tag>("tags");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshtokens");
    public IMongoCollection<MonthlyExpenseCategory> MonthlyExpenseCategories => _database.GetCollection<MonthlyExpenseCategory>("monthlyexpensecategories");
    public IMongoCollection<PushSubscription> PushSubscriptions => _database.GetCollection<PushSubscription>("pushsubscriptions");
    public IMongoCollection<House> Houses => _database.GetCollection<House>("houses");
    public IMongoCollection<DeviceToken> DeviceTokens => _database.GetCollection<DeviceToken>("devicetokens");
    public IMongoCollection<NotificationPreference> NotificationPreferences => _database.GetCollection<NotificationPreference>("notificationpreferences");
}
