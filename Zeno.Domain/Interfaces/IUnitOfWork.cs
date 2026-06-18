using System.Data;

namespace Zeno.Domain.Interfaces;

public interface IUnitOfWork
{
    IDbTransaction? Transaction { get; }
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}