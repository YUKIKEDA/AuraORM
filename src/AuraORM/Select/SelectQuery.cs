namespace AuraORM;

/// <summary>
/// A SELECT statement that has its projection: it can be rendered with <see cref="ToSql"/> and, when it is
/// <see cref="Executable"/>, run with the methods of <see cref="SelectExecution"/> (<c>docs/design.ja.md</c> §12.2, §12.3).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public abstract class SelectQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected SelectQuery(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
    {
        Session = session;
        State = state;
        Factory = factory;
    }

    /// <summary>
    /// Gets the session that runs the statement, or <see langword="null"/> for a <see cref="SqlOnly"/> statement.
    /// </summary>
    internal IStatementRunner? Session { get; }

    internal SelectState State { get; }

    internal RowFactory<TResult> Factory { get; }

    /// <summary>
    /// Returns the SQL text and the parameters of the statement, exactly as they are sent. No connection is used.
    /// </summary>
    /// <returns>The statement.</returns>
    public SqlStatement ToSql() => SqlRenderContext.Render(DialectRenderer.For<TDialect>(), new SelectNode(State));
}
