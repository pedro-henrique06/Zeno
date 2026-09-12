namespace Zeno.Domain.Interfaces;

public interface IUnitOfWork
{
    object? Transaction { get; }
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
