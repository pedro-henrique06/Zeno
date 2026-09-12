using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ZenoMongoContext _context;

    public UserRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.Find(x => x.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByProviderAsync(string provider, string providerId)
    {
        var providerEnum = Enum.Parse<OAuthProvider>(provider);
        return await _context.Users.Find(x => x.Provider == providerEnum && x.ProviderId == providerId).FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        await _context.Users.InsertOneAsync(user);
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var count = await _context.Users.CountDocumentsAsync(x => x.Email == email);
        return count > 0;
    }

    public async Task<bool> EmailExistsForOtherUserAsync(string email, Guid userId)
    {
        var count = await _context.Users.CountDocumentsAsync(x => x.Email == email && x.Id != userId);
        return count > 0;
    }

    public async Task<User> UpdateProfileAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
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
