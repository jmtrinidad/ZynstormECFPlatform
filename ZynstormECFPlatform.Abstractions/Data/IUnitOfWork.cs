using System.Data;

namespace ZynstormECFPlatform.Abstractions.Data;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }

    IDbTransaction? ActiveTransaction { get; }

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);

    Task BeginAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}