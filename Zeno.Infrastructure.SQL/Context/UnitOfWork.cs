using Zeno.Domain.Interfaces;

namespace Zeno.Infrastructure.SQL.Context;

public class UnitOfWork : IUnitOfWork
{
    public object? Transaction => null;

    public Task BeginAsync()
    {
        // MongoDB handles transactions differently - no-op for basic operations
        return Task.CompletedTask;
    }

    public Task CommitAsync()
    {
        // MongoDB handles transactions differently - no-op for basic operations
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        // MongoDB handles transactions differently - no-op for basic operations
        return Task.CompletedTask;
    }
}
