using Dapper;
using Zeno.Domain.Interfaces;
using Zeno.Domain.User;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ZenoDbContext _context;

    public UserRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Name, Email, PasswordHash, CreatedAt 
                             FROM Users WHERE Id = @Id";

        return await _context.Connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"SELECT Id, Name, Email, PasswordHash, CreatedAt 
                             FROM Users WHERE Email = @Email";

        return await _context.Connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User> CreateAsync(User user)
    {
        const string sql = @"INSERT INTO Users (Id, Name, Email, PasswordHash, CreatedAt) 
                             VALUES (@Id, @Name, @Email, @PasswordHash, @CreatedAt)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            user.Id,
            user.Name,
            user.Email,
            user.PasswordHash,
            user.CreatedAt
        });

        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = @"SELECT COUNT(1) FROM Users WHERE Email = @Email";

        var count = await _context.Connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }
}
