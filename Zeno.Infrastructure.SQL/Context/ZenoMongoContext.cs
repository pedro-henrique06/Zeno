using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Zeno.Domain.User;
using Zeno.Domain.Entry;
using Zeno.Domain.Auth;
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
    }

    public ZenoMongoContext(string connectionString, string databaseName = "zeno_db")
    {
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);
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
