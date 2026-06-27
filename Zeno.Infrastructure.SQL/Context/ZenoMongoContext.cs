using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Zeno.Application.Interfaces;
using Zeno.Domain.User;
using Zeno.Domain.Entry;
using Zeno.Domain.Auth;
using Zeno.Domain.Enum;
using Zeno.Infrastructure.SQL.Serialization;
using Tag = Zeno.Domain.Tag.Tag;
using MonthlyExpenseCategory = Zeno.Domain.MonthlyExpenseCategory.MonthlyExpenseCategory;

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

        if (!BsonClassMap.IsClassMapRegistered(typeof(Entry)))
        {
            BsonClassMap.RegisterClassMap<Entry>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(e => e.Value).SetSerializer(encryptedDecimal);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.AutoMap();
                cm.GetMemberMap(u => u.DailyBudget).SetSerializer(new NullableSerializer<decimal>(encryptedDecimal));
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
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Entry> Entries => _database.GetCollection<Entry>("entries");
    public IMongoCollection<Tag> Tags => _database.GetCollection<Tag>("tags");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshtokens");
    public IMongoCollection<MonthlyExpenseCategory> MonthlyExpenseCategories => _database.GetCollection<MonthlyExpenseCategory>("monthlyexpensecategories");
}
