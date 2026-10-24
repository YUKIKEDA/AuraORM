using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// Runs DDL statements (design §13.1).
/// </summary>
public static class DdlExecution
{
    /// <summary>
    /// Runs the statement, or the statements one by one in order. The first error stops the run, and the statements
    /// before it are not undone; run them in a transaction to undo them (on a dialect with
    /// <see cref="ITransactionalDdlDialect"/>).
    /// </summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <param name="statement">The DDL statement.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>A task that completes when every statement has run.</returns>
    /// <exception cref="InvalidOperationException">
    /// A name, a type, or a key is beyond the dialect's limit, or the table declares what the dialect cannot write
    /// (design §13, §14.3). Nothing runs.
    /// </exception>
    public static async Task ExecuteAsync<TDialect>(this DdlStatement<TDialect, Executable> statement, CancellationToken cancellationToken = default)
        where TDialect : IDialect
    {
        ArgumentNullException.ThrowIfNull(statement);
        ImmutableArray<SqlStatement> statements = statement.RenderAll();
        foreach (SqlStatement sql in statements)
        {
            _ = await Executor.ExecuteAsync(statement.Session!, sql, cancellationToken).ConfigureAwait(false);
        }
    }
}
