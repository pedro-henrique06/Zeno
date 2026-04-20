using Dapper;
using Zeno.Domain.Entry;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class EntryRepository : IEntryRepository
{
    private readonly ZenoDbContext _context;

    public EntryRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<Entry?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT Id, Title, Value, Type, Description, Category, Date, WalletId 
                             FROM Entries WHERE Id = @Id";

        return await _context.Connection.QueryFirstOrDefaultAsync<Entry>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Entry>> GetByMonthAsync(int month, int year, Guid walletId)
    {
        const string sql = @"SELECT Id, Title, Value, Type, Description, Category, Date, WalletId 
                             FROM Entries 
                             WHERE MONTH(Date) = @Month AND YEAR(Date) = @Year AND WalletId = @WalletId";

        return await _context.Connection.QueryAsync<Entry>(sql, new { Month = month, Year = year, WalletId = walletId });
    }

    public async Task<Entry> CreateAsync(Entry entry)
    {
        const string sql = @"INSERT INTO Entries (Id, Title, Value, Type, Description, Category, Date, WalletId) 
                             VALUES (@Id, @Title, @Value, @Type, @Description, @Category, @Date, @WalletId)";

        await _context.Connection.ExecuteAsync(sql, new
        {
            entry.Id,
            entry.Title,
            entry.Value,
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
            entry.Value,
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
}
