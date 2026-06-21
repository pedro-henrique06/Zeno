using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Zeno.Domain.User;
using Zeno.Domain.Entry;
using Zeno.Domain.FinancialGoal;
using Zeno.Domain.Debt;
using Zeno.Domain.CustomCategory;
using Zeno.Domain.Auth;

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

    /// <summary>
    /// Creates indexes for all collections. Call this method during application startup or migration.
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        // User indexes
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true }));
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Provider).Ascending(x => x.ProviderId)));

        // Entry indexes
        await Entries.Indexes.CreateOneAsync(new CreateIndexModel<Entry>(
            Builders<Entry>.IndexKeys.Ascending(x => x.WalletId).Ascending(x => x.Date)));
        await Entries.Indexes.CreateOneAsync(new CreateIndexModel<Entry>(
            Builders<Entry>.IndexKeys.Ascending(x => x.Date)));

        // RefreshToken indexes
        await RefreshTokens.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(x => x.Token),
            new CreateIndexOptions { Unique = true }));
        await RefreshTokens.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(x => x.UserId)));

        // FinancialGoal indexes
        await FinancialGoals.Indexes.CreateOneAsync(new CreateIndexModel<FinancialGoal>(
            Builders<FinancialGoal>.IndexKeys.Ascending(x => x.UserId)));

        // Debt indexes
        await Debts.Indexes.CreateOneAsync(new CreateIndexModel<Debt>(
            Builders<Debt>.IndexKeys.Ascending(x => x.UserId)));

        // Category indexes
        await Categories.Indexes.CreateOneAsync(new CreateIndexModel<Category>(
            Builders<Category>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.Type)));
        await CategoryRules.Indexes.CreateOneAsync(new CreateIndexModel<CategoryRule>(
            Builders<CategoryRule>.IndexKeys.Ascending(x => x.UserId)));
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Entry> Entries => _database.GetCollection<Entry>("entries");
    public IMongoCollection<FinancialGoal> FinancialGoals => _database.GetCollection<FinancialGoal>("financialgoals");
    public IMongoCollection<Debt> Debts => _database.GetCollection<Debt>("debts");
    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");
    public IMongoCollection<CategoryRule> CategoryRules => _database.GetCollection<CategoryRule>("categoryrules");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshtokens");
}
