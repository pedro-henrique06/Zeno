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
        const string sql = @"SELECT id, name, email, phone, document, birthdate, provider, providerid, passwordhash, createdat, updatedat, emailverified
                             FROM users WHERE id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToUser(row);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"SELECT id, name, email, phone, document, birthdate, provider, providerid, passwordhash, createdat, updatedat, emailverified
                             FROM users WHERE email = @Email";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = email });
        return row is null ? null : MapToUser(row);
    }

    public async Task<User?> GetByProviderAsync(string provider, string providerId)
    {
        const string sql = @"SELECT id, name, email, phone, document, birthdate, provider, providerid, passwordhash, createdat, updatedat, emailverified
                             FROM users WHERE provider = @Provider AND providerid = @ProviderId";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Provider = provider, ProviderId = providerId });
        return row is null ? null : MapToUser(row);
    }

    public async Task<User> CreateAsync(User user)
    {
        const string sql = @"INSERT INTO users (id, name, email, phone, document, birthdate, provider, providerid, passwordhash, createdat, updatedat, emailverified)
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
        const string sql = "SELECT COUNT(1) FROM users WHERE email = @Email";
        var count = await _context.Connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }

    public async Task<bool> EmailExistsForOtherUserAsync(string email, Guid userId)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE email = @Email AND id != @UserId";
        var count = await _context.Connection.ExecuteScalarAsync<int>(sql, new { Email = email, UserId = userId });
        return count > 0;
    }

    public async Task<User> UpdateProfileAsync(User user)
    {
        const string sql = @"UPDATE users
                             SET name = @Name, email = @Email, phone = @Phone, document = @Document, birthdate = @BirthDate, updatedat = @UpdatedAt
                             WHERE id = @Id";

        await _context.Connection.ExecuteAsync(sql, new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Document,
            BirthDate = user.BirthDate.HasValue ? user.BirthDate.Value : (DateTime?)null,
            UpdatedAt = DateTime.UtcNow
        });

        return user;
    }

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash)
    {
        const string sql = @"UPDATE users SET passwordhash = @PasswordHash, updatedat = @UpdatedAt WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = userId, PasswordHash = passwordHash, UpdatedAt = DateTime.UtcNow });
    }

    private User MapToUser(dynamic row)
    {
        Guid id = Guid.Empty;
        if (row.id is Guid rowId)
            id = rowId;
        else if (row.id is string rowIdStr && Guid.TryParse(rowIdStr, out Guid parsedId))
            id = parsedId;

        string? passwordHash = null;
        if (row.passwordhash != null)
        {
            if (row.passwordhash is string strHash)
                passwordHash = strHash;
            else if (row.passwordhash is byte[] bytes)
                passwordHash = Encoding.UTF8.GetString(bytes);
            else
                passwordHash = Convert.ToString(row.passwordhash);
        }

        return new User
        {
            Id = id,
            Name = row.name?.ToString() ?? string.Empty,
            Email = row.email?.ToString() ?? string.Empty,
            Phone = row.phone as string,
            Document = row.document as string,
            BirthDate = row.birthdate is DateTime dt ? dt : (row.birthdate is not null ? DateTime.Parse(row.birthdate.ToString()) : (DateTime?)null),
            Provider = row.provider is int pi ? (OAuthProvider)pi : OAuthProvider.None,
            ProviderId = row.providerid as string,
            PasswordHash = passwordHash,
            CreatedAt = row.createdat is DateTime ct ? ct : DateTime.UtcNow,
            UpdatedAt = row.updatedat is DateTime ut ? ut : (DateTime?)null,
            EmailVerified = row.emailverified is bool eb ? eb : false
        };
    }
}