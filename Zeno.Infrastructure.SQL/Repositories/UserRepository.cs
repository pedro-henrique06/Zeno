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
        const string sql = @"SELECT Id, Name, Email, Phone, Document, BirthDate, Provider, ProviderId, PasswordHash, CreatedAt, UpdatedAt, EmailVerified
                             FROM Users WHERE Id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToUser(row);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"SELECT Id, Name, Email, Phone, Document, BirthDate, Provider, ProviderId, PasswordHash, CreatedAt, UpdatedAt, EmailVerified
                             FROM Users WHERE Email = @Email";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = email });
        return row is null ? null : MapToUser(row);
    }

    public async Task<User?> GetByProviderAsync(string provider, string providerId)
    {
        const string sql = @"SELECT Id, Name, Email, Phone, Document, BirthDate, Provider, ProviderId, PasswordHash, CreatedAt, UpdatedAt, EmailVerified
                             FROM Users WHERE Provider = @Provider AND ProviderId = @ProviderId";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Provider = provider, ProviderId = providerId });
        return row is null ? null : MapToUser(row);
    }

    public async Task<User> CreateAsync(User user)
    {
        const string sql = @"INSERT INTO Users (Id, Name, Email, Phone, Document, BirthDate, Provider, ProviderId, PasswordHash, CreatedAt, UpdatedAt, EmailVerified)
                             VALUES (@Id, @Name, @Email, @Phone, @Document, @BirthDate, @Provider, @ProviderId, @PasswordHash, @CreatedAt, @UpdatedAt, @EmailVerified)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Document,
            BirthDate = user.BirthDate.HasValue ? user.BirthDate.Value : (DateTime?)null,
            Provider = (int)user.Provider,
            user.ProviderId,
            user.PasswordHash,
            user.CreatedAt,
            user.UpdatedAt,
            user.EmailVerified
        });

        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = @"SELECT COUNT(1) FROM Users WHERE Email = @Email";
        var count = await _context.Connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }

    private User MapToUser(dynamic row)
    {
        return new User
        {
            Id = row.Id,
            Name = row.Name,
            Email = row.Email,
            Phone = row.Phone,
            Document = row.Document,
            BirthDate = row.BirthDate,
            Provider = (OAuthProvider)(int)row.Provider,
            ProviderId = row.ProviderId,
            PasswordHash = row.PasswordHash,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            EmailVerified = row.EmailVerified
        };
    }
}