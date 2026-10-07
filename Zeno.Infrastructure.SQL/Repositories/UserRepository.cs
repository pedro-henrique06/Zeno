using MongoDB.Bson;
using MongoDB.Driver;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ZenoMongoContext _context;
    private readonly IEmailBlindIndex _emailIndex;

    public UserRepository(ZenoMongoContext context, IEmailBlindIndex emailIndex)
    {
        _context = context;
        _emailIndex = emailIndex;
    }

    // The e-mail is stored encrypted, so lookups go through the blind index (EmailHash). Users that have
    // not been through the encryption migration yet still have the e-mail in plain text and no hash, so
    // they are found by a raw query on the plain value (the typed filter would encrypt the search value).
    private FilterDefinition<User> ByEmail(string email) =>
        Builders<User>.Filter.Eq(x => x.EmailHash, _emailIndex.Compute(email));

    private FilterDefinition<User> ById(BsonValue id) => new BsonDocumentFilterDefinition<User>(new BsonDocument("_id", id));

    private async Task<List<BsonValue>> LegacyIdsByEmailAsync(string email)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("Email", email.Trim());
        var docs = await _context.GetRawCollection("users").Find(filter).Project(new BsonDocument("_id", 1)).ToListAsync();
        return docs.Select(d => d["_id"]).ToList();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _context.Users.Find(ByEmail(email)).FirstOrDefaultAsync();
        if (user is not null)
            return user;

        var legacyIds = await LegacyIdsByEmailAsync(email);
        return legacyIds.Count == 0
            ? null
            : await _context.Users.Find(ById(legacyIds[0])).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByProviderAsync(string provider, string providerId)
    {
        var providerEnum = Enum.Parse<OAuthProvider>(provider);
        return await _context.Users.Find(x => x.Provider == providerEnum && x.ProviderId == providerId).FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.EmailHash = _emailIndex.Compute(user.Email);
        await _context.Users.InsertOneAsync(user);
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        if (await _context.Users.CountDocumentsAsync(ByEmail(email)) > 0)
            return true;

        return (await LegacyIdsByEmailAsync(email)).Count > 0;
    }

    public async Task<bool> EmailExistsForOtherUserAsync(string email, Guid userId)
    {
        var byHash = Builders<User>.Filter.And(ByEmail(email), Builders<User>.Filter.Ne(x => x.Id, userId));
        if (await _context.Users.CountDocumentsAsync(byHash) > 0)
            return true;

        foreach (var id in await LegacyIdsByEmailAsync(email))
        {
            var other = await _context.Users.Find(ById(id)).FirstOrDefaultAsync();
            if (other is not null && other.Id != userId)
                return true;
        }

        return false;
    }

    public async Task<User> UpdateProfileAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        user.EmailHash = _emailIndex.Compute(user.Email);
        var filter = Builders<User>.Filter.Eq(x => x.Id, user.Id);
        await _context.Users.ReplaceOneAsync(filter, user);
        return user;
    }

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Id, userId);
        var update = Builders<User>.Update
            .Set(x => x.PasswordHash, passwordHash)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _context.Users.UpdateOneAsync(filter, update);
    }
}
