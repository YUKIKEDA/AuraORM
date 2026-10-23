namespace AuraORM;

/// <summary>
/// The run methods of an <see cref="Executable"/> SELECT statement (<c>docs/design.ja.md</c> §12.2). A <see cref="SqlOnly"/>
/// statement, from a <see cref="SqlBuilder{TDialect}"/>, has none of them, so running it does not compile.
/// </summary>
public static class SelectExecution
{
    /// <summary>
    /// Runs the statement and reads every row.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public static Task<List<TResult>> ToListAsync<TDialect, TResult>(
        this SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        ArgumentNullException.ThrowIfNull(query);
        return Executor.ListAsync(query.Session!, query.ToSql(), query.Factory, cancellationToken);
    }

    /// <summary>
    /// Runs the statement and reads the rows one at a time. The connection and the reader are held while the rows are
    /// enumerated and released when the enumeration ends or stops.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public static IAsyncEnumerable<TResult> AsAsyncEnumerable<TDialect, TResult>(
        this SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        ArgumentNullException.ThrowIfNull(query);
        return Executor.StreamAsync(query.Session!, query.ToSql(), query.Factory, cancellationToken);
    }

    /// <summary>
    /// Runs the statement and returns the first row. No <c>LIMIT</c> is added; the reader is closed after the first row, and
    /// the command is cancelled first when rows are left, so that the provider does not read them to the end
    /// (<c>docs/design.ja.md</c> §12.2). Write <c>Limit(1)</c> to have the database stop after one row.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first row.</returns>
    /// <exception cref="InvalidOperationException">The statement returned no row.</exception>
    public static async Task<TResult> FirstAsync<TDialect, TResult>(
        this SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        await foreach (TResult row in query.AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            return row;
        }

        throw new InvalidOperationException("The statement returned no row.");
    }

    /// <summary>
    /// Runs the statement and returns the first row, or <see langword="default"/> when there is none. No <c>LIMIT</c> is added.
    /// For a value type (<see cref="long"/>, a tuple), no row and a row of default values give the same result; use
    /// <c>FirstAsync</c>, or read a nullable type, to tell them apart.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first row, or <see langword="default"/>.</returns>
    public static async Task<TResult?> FirstOrDefaultAsync<TDialect, TResult>(
        this SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        await foreach (TResult row in query.AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            return row;
        }

        return default;
    }

    /// <summary>
    /// Runs the statement and returns its only row. No <c>LIMIT</c> is added; a second row is read to check that there is none.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The only row.</returns>
    /// <exception cref="InvalidOperationException">The statement returned no row, or more than one.</exception>
    public static async Task<TResult> SingleAsync<TDialect, TResult>(
        this SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        (bool found, TResult? row) = await SingleCoreAsync(query, cancellationToken).ConfigureAwait(false);
        return found ? row! : throw new InvalidOperationException("The statement returned no row.");
    }

    /// <summary>
    /// Runs the statement and returns its only row, or <see langword="default"/> when there is none. No <c>LIMIT</c> is added.
    /// For a value type, no row and a row of default values give the same result; use <c>SingleAsync</c>, or read a nullable
    /// type, to tell them apart.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The only row, or <see langword="default"/>.</returns>
    /// <exception cref="InvalidOperationException">The statement returned more than one row.</exception>
    public static async Task<TResult?> SingleOrDefaultAsync<TDialect, TResult>(
        this SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        (_, TResult? row) = await SingleCoreAsync(query, cancellationToken).ConfigureAwait(false);
        return row;
    }

    private static async Task<(bool Found, TResult? Row)> SingleCoreAsync<TDialect, TResult>(
        SelectQuery<TDialect, Executable, TResult> query,
        CancellationToken cancellationToken
    )
        where TDialect : IDialect
    {
        bool found = false;
        TResult? single = default;
        await foreach (TResult row in query.AsAsyncEnumerable(cancellationToken).ConfigureAwait(false))
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
