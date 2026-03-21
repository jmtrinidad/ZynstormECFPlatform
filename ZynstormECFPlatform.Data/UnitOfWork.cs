using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data;

public class UnitOfWork(StorageContext context) : IUnitOfWork
{
    private readonly StorageContext _context = context;

    public IDbConnection Connection => _context.Database.GetDbConnection();

    public IDbTransaction? ActiveTransaction =>
        _context.Database.CurrentTransaction?.GetDbTransaction();

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await BeginAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await operation(cancellationToken).ConfigureAwait(false);
                await CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        });
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await BeginAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var result = await operation(cancellationToken).ConfigureAwait(false);
                await CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        });
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return;
        }

        await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            return;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _context.Database.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            return;
        }

        await _context.Database.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}