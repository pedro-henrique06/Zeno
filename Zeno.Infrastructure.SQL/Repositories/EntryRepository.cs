using Dapper;
using Zeno.Application.Interfaces;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class EntryRepository : IEntryRepository
{
    private readonly ZenoDbContext _context;
    private readonly IEncryptionService _encryption;

    public EntryRepository(ZenoDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<Entry?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Title, Value, Type, Description, Category, Date, WalletId
                             FROM Entries WHERE Id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToEntry(row);
    }

    public async Task<IEnumerable<Entry>> GetByMonthAsync(int month, int year, Guid walletId)
    {
        const string sql = @"SELECT Id, Title, Value, Type, Description, Category, Date, WalletId
                             FROM Entries
                             WHERE EXTRACT(MONTH FROM Date) = @Month AND EXTRACT(YEAR FROM Date) = @Year AND WalletId = @WalletId";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { Month = month, Year = year, WalletId = walletId });
        return rows.Select(r => MapToEntry(r)).Cast<Entry>();
    }

    public async Task<Entry> CreateAsync(Entry entry)
    {
        const string sql = @"INSERT INTO Entries (Id, Title, Value, Type, Description, Category, Date, WalletId)
                             VALUES (@Id, @Title, @Value, @Type, @Description, @Category, @Date, @WalletId)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            entry.Id,
            entry.Title,
            Value = _encryption.EncryptDecimal(entry.Value),
            Type = (int)entry.Type,
            entry.Description,
            Category = (int)entry.Category,
            entry.Date,
            entry.WalletId
        });

        return entry;
    }

    public async Task<Entry> UpdateAsync(Entry entry)
    {
        const string sql = @"UPDATE Entries
                             SET Title = @Title, Value = @Value, Type = @Type,
                                 Description = @Description, Category = @Category, Date = @Date, WalletId = @WalletId
                             WHERE Id = @Id";

        await _context.Connection.ExecuteAsync(sql, new
        {
            entry.Id,
            entry.Title,
            Value = _encryption.EncryptDecimal(entry.Value),
            Type = (int)entry.Type,
            entry.Description,
            Category = (int)entry.Category,
            entry.Date,
            entry.WalletId
        });

        return entry;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM Entries WHERE Id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private Entry MapToEntry(dynamic row)
    {
        var value = row.Value is string s
            ? _encryption.DecryptDecimal(s)
            : (decimal)row.Value;
        return new Entry
        {
            Id = row.Id,
            Title = row.Title,
            Value = value,
            Type = (EntryType)(int)row.Type,
            Description = row.Description,
            Category = (Category)(int)row.Category,
            Date = row.Date,
            WalletId = row.WalletId
        };
    }
}