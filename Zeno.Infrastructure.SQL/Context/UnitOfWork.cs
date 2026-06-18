using System.Data;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;

namespace Zeno.Infrastructure.SQL.Context;

public class UnitOfWork : IUnitOfWork
{
    private readonly ZenoDbContext _context;

    public UnitOfWork(ZenoDbContext context)
    {
        _context = context;
    }

    public IDbTransaction? Transaction => _context.Transaction;

    public async Task BeginAsync()
    {
        await _context.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _context.CommitTransactionAsync();
    }

    public async Task RollbackAsync()
    {
        await _context.RollbackTransactionAsync();
    }
}