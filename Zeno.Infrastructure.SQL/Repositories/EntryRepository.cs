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
        const string sql = @"SELECT id, title, value, type, description, category, date, wallet_id
                             FROM entries WHERE id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToEntry(row);
    }

    public async Task<IEnumerable<Entry>> GetByMonthAsync(int month, int year, Guid walletId)
    {
        const string sql = @"SELECT id, title, value, type, description, category, date, wallet_id
                             FROM entries
                             WHERE EXTRACT(MONTH FROM date) = @Month AND EXTRACT(YEAR FROM date) = @Year AND wallet_id = @WalletId";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { Month = month, Year = year, WalletId = walletId });
        return rows.Select(r => MapToEntry(r)).Cast<Entry>();
    }

    public async Task<Entry> CreateAsync(Entry entry)
    {
        const string sql = @"INSERT INTO entries (id, title, value, type, description, category, date, wallet_id)
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
        const string sql = @"UPDATE entries
                             SET title = @Title, value = @Value, type = @Type,
                                 description = @Description, category = @Category, date = @Date, wallet_id = @WalletId
                             WHERE id = @Id";

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
        const string sql = @"DELETE FROM entries WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private Entry MapToEntry(dynamic row)
    {
        var value = row.value is string s
            ? _encryption.DecryptDecimal(s)
            : (decimal)row.value;
        return new Entry
        {
            Id = row.id,
            Title = row.title,
            Value = value,
            Type = (EntryType)(int)row.type,
            Description = row.description,
            Category = (Category)(int)row.category,
            Date = row.date,
            WalletId = row.wallet_id
        };
    }
}