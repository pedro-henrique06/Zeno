using System.Text;
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
        Console.WriteLine($"[MapToUser] DEBUG - PasswordHash type: {row.PasswordHash?.GetType().Name}, value: '{row.PasswordHash}', is null: {row.PasswordHash == null}");

        string? passwordHash = null;
        if (row.PasswordHash != null)
        {
            if (row.PasswordHash is string strHash)
                passwordHash = strHash;
            else if (row.PasswordHash is byte[] bytes)
                passwordHash = Encoding.UTF8.GetString(bytes);
            else
                passwordHash = Convert.ToString(row.PasswordHash);
        }

        Console.WriteLine($"[MapToUser] DEBUG - Final passwordHash: '{passwordHash}'");

        return new User
        {
            Id = Guid.TryParse((string)row.Id, out Guid id) ? id : Guid.Empty,
            Name = row.Name?.ToString() ?? string.Empty,
            Email = row.Email?.ToString() ?? string.Empty,
            Phone = row.Phone as string,
            Document = row.Document as string,
            BirthDate = row.BirthDate is DateTime dt ? dt : (row.BirthDate is not null ? DateTime.Parse(row.BirthDate.ToString()) : (DateTime?)null),
            Provider = row.Provider is int pi ? (OAuthProvider)pi : OAuthProvider.None,
            ProviderId = row.ProviderId as string,
            PasswordHash = passwordHash,
            CreatedAt = row.CreatedAt is DateTime ct ? ct : DateTime.UtcNow,
            UpdatedAt = row.UpdatedAt is DateTime ut ? ut : (DateTime?)null,
            EmailVerified = row.EmailVerified is bool eb ? eb : false
        };
    }
}