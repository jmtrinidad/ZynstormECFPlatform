using System.Data;

namespace ZynstormECFPlatform.Abstractions.Data;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }

    IDbTransaction? ActiveTransaction { get; }

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual que la sobrecarga sin nivel, que usa Serializable. Con Serializable (y
    /// RepeatableRead) Postgres toma el snapshot en la primera sentencia, antes de que un
    /// pg_advisory_xact_lock termine de esperar; con ReadCommitted cada sentencia ve lo que ya
    /// se confirmó.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    Task BeginAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}