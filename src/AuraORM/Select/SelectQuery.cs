namespace AuraORM;

/// <summary>
/// A SELECT statement that has its projection: it can be run, or rendered with <see cref="ToSql"/>
/// (<c>docs/design.ja.md</c> §12.2, §12.3).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public abstract class SelectQuery<TDialect, TResult>
    where TDialect : IDialect
{
    private protected SelectQuery(IStatementRunner session, SelectState state, RowFactory<TResult> factory)
    {
        Session = session;
        State = state;
        Factory = factory;
    }

    private protected IStatementRunner Session { get; }

    private protected SelectState State { get; }

    private protected RowFactory<TResult> Factory { get; }

    /// <summary>
    /// Returns the SQL text and the parameters of the statement, exactly as they are sent. No connection is used.
    /// </summary>
    /// <returns>The statement.</returns>
    public SqlStatement ToSql() => SqlRenderContext.Render(DialectRenderer.For<TDialect>(), new SelectNode(State));

    /// <summary>
    /// Runs the statement and reads every row.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<TResult>> ToListAsync(CancellationToken cancellationToken = default) =>
        Executor.ListAsync(Session, ToSql(), Factory, cancellationToken);

    /// <summary>
    /// Runs the statement and reads the rows one at a time. The connection and the reader are held while the rows are
    /// enumerated and released when the enumeration ends or stops.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public IAsyncEnumerable<TResult> AsAsyncEnumerable(CancellationToken cancellationToken = default) =>
        Executor.StreamAsync(Session, ToSql(), Factory, cancellationToken);

    /// <summary>
    /// Runs the statement and returns the first row. No <c>LIMIT</c> is added; the reader is closed after the first row
    /// (<c>docs/design.ja.md</c> §12.2).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first row.</returns>
    /// <exception cref="InvalidOperationException">The statement returned no row.</exception>
    public async Task<TResult> FirstAsync(CancellationToken cancellationToken = default)
    {
        await foreach (TResult row in AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            return row;
        }

        throw new InvalidOperationException("The statement returned no row.");
    }

    /// <summary>
    /// Runs the statement and returns the first row, or <see langword="default"/> when there is none. No <c>LIMIT</c> is added.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first row, or <see langword="default"/>.</returns>
    public async Task<TResult?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        await foreach (TResult row in AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            return row;
        }

        return default;
    }

    /// <summary>
    /// Runs the statement and returns its only row. No <c>LIMIT</c> is added; a second row is read to check that there is none.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The only row.</returns>
    /// <exception cref="InvalidOperationException">The statement returned no row, or more than one.</exception>
    public async Task<TResult> SingleAsync(CancellationToken cancellationToken = default)
    {
        (bool found, TResult? row) = await SingleCoreAsync(cancellationToken).ConfigureAwait(false);
        return found ? row! : throw new InvalidOperationException("The statement returned no row.");
    }

    /// <summary>
    /// Runs the statement and returns its only row, or <see langword="default"/> when there is none. No <c>LIMIT</c> is added.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The only row, or <see langword="default"/>.</returns>
    /// <exception cref="InvalidOperationException">The statement returned more than one row.</exception>
    public async Task<TResult?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        (_, TResult? row) = await SingleCoreAsync(cancellationToken).ConfigureAwait(false);
        return row;
    }

    private async Task<(bool Found, TResult? Row)> SingleCoreAsync(CancellationToken cancellationToken)
    {
        bool found = false;
        TResult? single = default;
        await foreach (TResult row in AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            if (found)
            {
                throw new InvalidOperationException("The statement returned more than one row.");
            }

            found = true;
            single = row;
        }

        return (found, single);
    }
}
