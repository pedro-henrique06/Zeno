using System.Data;
using Dapper;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
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
        const string sql = @"SELECT id, title, value, type, description, category, date, walletid
                             FROM entries WHERE id = @Id";

        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToEntry(row);
    }

    public async Task<IEnumerable<Entry>> GetByMonthAsync(int month, int year, Guid walletId)
    {
        const string sql = @"SELECT id, title, value, type, description, category, date, walletid
                             FROM entries
                             WHERE EXTRACT(MONTH FROM date) = @Month AND EXTRACT(YEAR FROM date) = @Year AND walletid = @WalletId";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { Month = month, Year = year, WalletId = walletId });
        return rows.Select(r => MapToEntry(r)).Cast<Entry>();
    }

    public async Task<(IEnumerable<Entry> Items, int TotalCount)> GetByMonthPagedAsync(
        int month, int year, Guid walletId, EntryType? type, Category? category, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        var whereClause = @"WHERE EXTRACT(MONTH FROM date) = @Month AND EXTRACT(YEAR FROM date) = @Year AND walletid = @WalletId";
        if (type.HasValue)
            whereClause += " AND type = @Type";
        if (category.HasValue)
            whereClause += " AND category = @Category";

        var countSql = $"SELECT COUNT(*) FROM entries {whereClause}";
        var dataSql = $@"SELECT id, title, value, type, description, category, date, walletid
                         FROM entries
                         {whereClause}
                         ORDER BY date DESC
                         LIMIT @PageSize OFFSET @Offset";

        using var multi = await _context.Connection.QueryMultipleAsync(countSql + ";" + dataSql, new
        {
            Month = month,
            Year = year,
            WalletId = walletId,
            Type = type.HasValue ? (int)type.Value : (int?)null,
            Category = category.HasValue ? (int)category.Value : (int?)null,
            PageSize = pageSize,
            Offset = offset
        });

        var totalCount = await multi.ReadSingleAsync<int>();
        var rows = await multi.ReadAsync<dynamic>();
        var items = rows.Select(r => MapToEntry(r)).Cast<Entry>();

        return (items, totalCount);
    }

    public async Task<decimal> GetTotalByTypeAndWalletAsync(int month, int year, Guid walletId, int type)
    {
        var sql = @"SELECT COALESCE(SUM(value), 0)
                    FROM entries
                    WHERE EXTRACT(MONTH FROM date) = @Month
                      AND EXTRACT(YEAR FROM date) = @Year
                      AND walletid = @WalletId
                      AND type = @Type";

        return await _context.Connection.ExecuteScalarAsync<decimal>(sql, new { Month = month, Year = year, WalletId = walletId, Type = type });
    }

    public async Task<IEnumerable<(int Category, decimal Total)>> GetCategoryTotalsAsync(int month, int year, Guid walletId)
    {
        var sql = @"SELECT category, COALESCE(SUM(value), 0) as total
                    FROM entries
                    WHERE EXTRACT(MONTH FROM date) = @Month
                      AND EXTRACT(YEAR FROM date) = @Year
                      AND walletid = @WalletId
                      AND type = 1
                    GROUP BY category
                    ORDER BY total DESC";

        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { Month = month, Year = year, WalletId = walletId });
        return rows.Select(r => ((int)r.category, (decimal)r.total));
    }

    public async Task<Entry> CreateAsync(Entry entry, object? transaction = null)
    {
        const string sql = @"INSERT INTO entries (id, title, value, type, description, category, date, walletid)
                             VALUES (@Id, @Title, @Value, @Type, @Description, @Category, @Date, @WalletId)";

        var tx = transaction as IDbTransaction;
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
        }, tx);

        return entry;
    }

    public async Task<Entry> UpdateAsync(Entry entry, object? transaction = null)
    {
        const string sql = @"UPDATE entries
                             SET title = @Title, value = @Value, type = @Type,
                                 description = @Description, category = @Category, date = @Date, walletid = @WalletId
                             WHERE id = @Id";

        var tx = transaction as IDbTransaction;
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
        }, tx);

        return entry;
    }

    public async Task DeleteAsync(Guid id, object? transaction = null)
    {
        const string sql = @"DELETE FROM entries WHERE id = @Id";
        var tx = transaction as IDbTransaction;
        await _context.Connection.ExecuteAsync(sql, new { Id = id }, tx);
    }

    private Entry MapToEntry(dynamic row)
    {
        return new Entry
        {
            Id = row.id,
            Title = row.title,
            Value = (decimal)row.value,
            Type = (EntryType)(int)row.type,
            Description = row.description,
            Category = (Category)(int)row.category,
            Date = row.date,
            WalletId = row.walletid
        };
    }
}