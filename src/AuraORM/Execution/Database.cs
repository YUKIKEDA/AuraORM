using System.Data;
using System.Data.Common;

namespace AuraORM;

/// <summary>
/// A database: the entry point of every statement (<c>docs/design.ja.md</c> §12.1).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect: <see cref="SqliteDialect"/>, <see cref="PostgreSqlDialect"/>,
/// <see cref="MySqlDialect"/>, <see cref="MariaDbDialect"/>, or <see cref="SqlServerDialect"/>.</typeparam>
/// <remarks>
/// The data source or connection belongs to the caller; the database never disposes it. Built on a
/// <see cref="DbDataSource"/>, a database is safe to use from several threads. Built on a <see cref="DbConnection"/>, it is
/// not, like the connection.
/// </remarks>
public sealed class Database<TDialect> : DatabaseSession<TDialect>
    where TDialect : IDialect
{
    private readonly DbDataSource? _dataSource;
    private readonly DbConnection? _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="Database{TDialect}"/> class that takes a connection from a data source
    /// for each command.
    /// </summary>
    /// <param name="dataSource">The data source.</param>
    /// <param name="options">The settings, or <see langword="null"/> for the defaults.</param>
    public Database(DbDataSource dataSource, DatabaseOptions? options = null)
        : base(options ?? new DatabaseOptions())
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        _ = DialectRenderer.For<TDialect>();
        _dataSource = dataSource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Database{TDialect}"/> class that runs every command on one connection.
    /// A closed connection is opened for each command and closed after it; an open connection stays open.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="options">The settings, or <see langword="null"/> for the defaults.</param>
    public Database(DbConnection connection, DatabaseOptions? options = null)
        : base(options ?? new DatabaseOptions())
    {
        ArgumentNullException.ThrowIfNull(connection);
        _ = DialectRenderer.For<TDialect>();
        _connection = connection;
    }

    /// <summary>
    /// Starts a transaction. Statements of the returned transaction run in it; dispose it without
    /// <see cref="Transaction{TDialect}.CommitAsync"/> to roll it back.
    /// </summary>
    /// <param name="isolationLevel">The isolation level, or <see cref="IsolationLevel.Unspecified"/> for the provider's default.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction.</returns>
    public async Task<Transaction<TDialect>> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        CancellationToken cancellationToken = default
    )
    {
        ConnectionLease lease = await LeaseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            DbTransaction transaction = await lease.Connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
            return new Transaction<TDialect>(Options, lease, transaction);
        }
        catch
        {
            await lease.ReleaseAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Runs a body in a transaction: commits when the body returns, rolls back when it throws.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the transaction is committed.</returns>
    /// <exception cref="AggregateException">The body threw, and the rollback threw too; the body's exception is first.</exception>
    public async Task InTransactionAsync(Func<Transaction<TDialect>, Task> body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        _ = await InTransactionAsync(
                async tx =>
                {
                    await body(tx).ConfigureAwait(false);
                    return true;
                },
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a body in a transaction and returns its result: commits when the body returns, rolls back when it throws.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="body">The body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The body's result, after the transaction is committed.</returns>
    /// <exception cref="AggregateException">The body threw, and the rollback threw too; the body's exception is first.</exception>
    public async Task<TResult> InTransactionAsync<TResult>(
        Func<Transaction<TDialect>, Task<TResult>> body,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(body);
        Transaction<TDialect> transaction = await BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            TResult result;
            try
            {
                result = await body(transaction).ConfigureAwait(false);
            }
            catch (Exception bodyError)
            {
                await transaction.RollbackAfterAsync(bodyError).ConfigureAwait(false);
                throw;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    private protected override async ValueTask<ConnectionLease> LeaseAsync(CancellationToken cancellationToken)
    {
        if (_dataSource is not null)
        {
            DbConnection pooled = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return new ConnectionLease(pooled, null, ConnectionLease.Ownership.DisposeAfter);
        }

        if (_connection!.State == ConnectionState.Open)
        {
            return new ConnectionLease(_connection, null, ConnectionLease.Ownership.Borrowed);
        }

        await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return new ConnectionLease(_connection, null, ConnectionLease.Ownership.CloseAfter);
    }
}
