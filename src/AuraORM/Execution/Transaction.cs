using System.Data.Common;

namespace AuraORM;

/// <summary>
/// A transaction: statements created from it run in it (<c>docs/design.ja.md</c> §12.5). Dispose it without
/// <see cref="CommitAsync"/> to roll it back. Nested transactions are not available; savepoints are roadmap row T2.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
public sealed class Transaction<TDialect> : DatabaseSession<TDialect>, IAsyncDisposable
    where TDialect : IDialect
{
    private readonly ConnectionLease _lease;
    private readonly DbTransaction _transaction;
    private bool _completed;
    private bool _disposed;

    internal Transaction(DatabaseOptions options, ConnectionLease lease, DbTransaction transaction)
        : base(options)
    {
        _lease = lease;
        _transaction = transaction;
    }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the transaction is committed.</returns>
    /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        _completed = true;
        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rolls the transaction back.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the transaction is rolled back.</returns>
    /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        _completed = true;
        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rolls back if the transaction was neither committed nor rolled back, then releases its connection.
    /// </summary>
    /// <returns>A task that completes when the connection is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (!_completed)
            {
                _completed = true;
                await _transaction.RollbackAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            await _lease.ReleaseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Rolls back after the body of <see cref="Database{TDialect}.InTransactionAsync{TResult}"/> threw. A rollback that throws
    /// too does not hide the body's exception (<c>.cursor/rules/csharp.mdc</c>).
    /// </summary>
    /// <param name="bodyError">The body's exception.</param>
    /// <returns>A task that completes when the transaction is rolled back.</returns>
    /// <exception cref="AggregateException">The rollback threw; the body's exception is first.</exception>
    internal async Task RollbackAfterAsync(Exception bodyError)
    {
        if (_completed)
        {
            return;
        }

        try
        {
            await RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception rollbackError)
        {
            throw new AggregateException(bodyError, rollbackError);
        }
    }

    private protected override ValueTask<ConnectionLease> LeaseAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfCompleted();
        return ValueTask.FromResult(new ConnectionLease(_lease.Connection, _transaction, ConnectionLease.Ownership.Borrowed));
    }

    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The transaction was already committed or rolled back.");
        }
    }
}
