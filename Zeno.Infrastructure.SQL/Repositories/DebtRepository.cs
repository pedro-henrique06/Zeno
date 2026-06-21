using MongoDB.Driver;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Debt;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Repositories;

public class DebtRepository : IDebtRepository
{
    private readonly ZenoMongoContext _context;

    public DebtRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<Debt?> GetByIdAsync(Guid id)
    {
        return await _context.Debts.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Debt>> GetByUserAsync(Guid userId)
    {
        return await _context.Debts
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Debt> CreateAsync(Debt debt)
    {
        await _context.Debts.InsertOneAsync(debt);
        return debt;
    }

    public async Task<Debt> UpdateAsync(Debt debt)
    {
        var filter = Builders<Debt>.Filter.Eq(x => x.Id, debt.Id);
        await _context.Debts.ReplaceOneAsync(filter, debt);
        return debt;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.Debts.DeleteOneAsync(x => x.Id == id);
    }
}
