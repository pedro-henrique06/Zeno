using Dapper;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Recurring;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class RecurringEntryRepository : IRecurringEntryRepository
{
    private readonly ZenoDbContext _context;

    public RecurringEntryRepository(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<RecurringEntry?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, type, kind, category, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurrententries WHERE id = @Id";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        return row is null ? null : MapToRecurringEntry(row);
    }

    public async Task<RecurringEntry?> GetByIdAndUserAsync(Guid id, Guid userId)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, type, kind, category, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurrententries
                             WHERE id = @Id AND userid = @UserId";
        var row = await _context.Connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });
        return row is null ? null : MapToRecurringEntry(row);
    }

    public async Task<IEnumerable<RecurringEntry>> GetByUserAsync(Guid userId)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, type, kind, category, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurrententries
                             WHERE userid = @UserId
                             ORDER BY dayofmonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { UserId = userId });
        return rows.Select(r => MapToRecurringEntry(r)).Cast<RecurringEntry>();
    }

    public async Task<IEnumerable<RecurringEntry>> GetByWalletAsync(Guid walletId)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, type, kind, category, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurrententries
                             WHERE walletid = @WalletId
                             ORDER BY dayofmonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { WalletId = walletId });
        return rows.Select(r => MapToRecurringEntry(r)).Cast<RecurringEntry>();
    }

    public async Task<IEnumerable<RecurringEntry>> GetActiveByDayAsync(int dayOfMonth)
    {
        const string sql = @"SELECT id, userid, walletid, title, value, type, kind, category, dayofmonth, isactive, createdat, lastprocessedat
                             FROM recurrententries
                             WHERE isactive = 1 AND dayofmonth = @DayOfMonth";
        var rows = await _context.Connection.QueryAsync<dynamic>(sql, new { DayOfMonth = dayOfMonth });
        return rows.Select(r => MapToRecurringEntry(r)).Cast<RecurringEntry>();
    }

    public async Task<RecurringEntry> CreateAsync(RecurringEntry entry)
    {
        const string sql = @"INSERT INTO recurrententries (id, userid, walletid, title, value, type, kind, category, dayofmonth, isactive, createdat, lastprocessedat)
                             VALUES (@Id, @UserId, @WalletId, @Title, @Value, @Type, @Kind, @Category, @DayOfMonth, @IsActive, @CreatedAt, @LastProcessedAt)";
        await _context.Connection.ExecuteAsync(sql, new
        {
            entry.Id,
            entry.UserId,
            entry.WalletId,
            entry.Title,
            entry.Value,
            Type = (int)entry.Type,
            Kind = (int)entry.Kind,
            Category = (int)entry.Category,
            entry.DayOfMonth,
            IsActive = entry.IsActive,
            entry.CreatedAt,
            entry.LastProcessedAt
        });
        return entry;
    }

    public async Task<RecurringEntry> UpdateAsync(RecurringEntry entry)
    {
        const string sql = @"UPDATE recurrententries
                             SET title = @Title, value = @Value, type = @Type, kind = @Kind, category = @Category,
                                 dayofmonth = @DayOfMonth, isactive = @IsActive, lastprocessedat = @LastProcessedAt
                             WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new
        {
            entry.Id,
            entry.Title,
            entry.Value,
            Type = (int)entry.Type,
            Kind = (int)entry.Kind,
            Category = (int)entry.Category,
            entry.DayOfMonth,
            IsActive = entry.IsActive,
            entry.LastProcessedAt
        });
        return entry;
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM recurrententries WHERE id = @Id";
        await _context.Connection.ExecuteAsync(sql, new { Id = id });
    }

    private RecurringEntry MapToRecurringEntry(dynamic row)
    {
        return new RecurringEntry
        {
            Id = row.id,
            UserId = row.userid,
            WalletId = row.walletid,
            Title = row.title,
            Value = (decimal)row.value,
            Type = (EntryType)(int)row.type,
            Kind = (EntryKind)(int)row.kind,
            Category = (Category)(int)row.category,
            DayOfMonth = row.dayofmonth,
            IsActive = row.isactive,
            CreatedAt = row.createdat,
            LastProcessedAt = row.lastprocessedat
        };
    }
}
