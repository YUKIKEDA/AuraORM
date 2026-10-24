namespace AuraORM;

/// <summary>
/// A statement that returns rows: a SELECT with its projection, or an <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> with
/// <c>Returning</c>. It can be rendered with <see cref="ToSql"/> and, when it is <see cref="Executable"/>, run with the methods
/// of <see cref="QueryExecution"/> (<c>docs/design.ja.md</c> §9, §12.2, §12.3).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public abstract class ResultQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected ResultQuery(IStatementRunner? session, RowFactory<TResult> factory)
    {
        Session = session;
        Factory = factory;
    }

    /// <summary>
    /// Gets the session that runs the statement, or <see langword="null"/> for a <see cref="SqlOnly"/> statement.
    /// </summary>
    internal IStatementRunner? Session { get; }

    internal RowFactory<TResult> Factory { get; }

    /// <summary>
    /// Gets a value indicating whether a reader closed with rows left calls <c>DbCommand.Cancel()</c> first. A SELECT does;
    /// a statement with <c>Returning</c> does not, because a cancel can undo its change (§9, §12.2).
    /// </summary>
    internal abstract bool CancelsUnreadRows { get; }

    /// <summary>
    /// Returns the SQL text and the parameters of the statement, exactly as they are sent. No connection is used.
    /// </summary>
    /// <returns>The statement.</returns>
    public abstract SqlStatement ToSql();
}
