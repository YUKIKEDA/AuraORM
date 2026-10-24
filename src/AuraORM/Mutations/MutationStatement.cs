namespace AuraORM;

/// <summary>
/// An <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> statement that is complete: it can be rendered with <see cref="ToSql"/>
/// and, when it is <see cref="Executable"/>, run with <see cref="MutationExecution.ExecuteAsync"/>
/// (<c>docs/design.ja.md</c> §6–§8, §12.3).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
public abstract class MutationStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected MutationStatement(IStatementRunner? session)
    {
        Session = session;
    }

    /// <summary>
    /// Gets the session that runs the statement, or <see langword="null"/> for a <see cref="SqlOnly"/> statement.
    /// </summary>
    internal IStatementRunner? Session { get; }

    /// <summary>
    /// Returns the SQL text and the parameters of the statement, exactly as they are sent. No connection is used.
    /// </summary>
    /// <returns>The statement.</returns>
    /// <exception cref="InvalidOperationException">The statement breaks a rule that is checked when it is rendered
    /// (<c>docs/design.ja.md</c> §14.3), such as a subquery that reads the target on MySQL.</exception>
    /// <exception cref="ArgumentException">The statement has more parameters than the dialect accepts.</exception>
    public SqlStatement ToSql() => SqlRenderContext.Render(DialectRenderer.For<TDialect>(), CreateNode());

    private protected abstract SqlNode CreateNode();
}
