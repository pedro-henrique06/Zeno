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

    private User MapToUser(dynamic row)
    {
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
            Id = Guid.TryParse((string)row.id, out Guid id) ? id : Guid.Empty,
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