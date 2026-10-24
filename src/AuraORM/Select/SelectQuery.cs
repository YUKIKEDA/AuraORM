namespace AuraORM;

/// <summary>
/// A SELECT statement that has its projection: it can be rendered with <see cref="ToSql"/>, run with the methods of
/// <see cref="QueryExecution"/> when it is <see cref="Executable"/>, and used as a subquery or an operand of a set operation
/// (<c>docs/design.ja.md</c> §5.10, §5.11, §12.2, §12.3).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public abstract class SelectQuery<TDialect, TMode, TResult> : ResultQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected SelectQuery(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, factory)
    {
        State = state;
    }

    internal SelectState State { get; }

    /// <inheritdoc />
    internal override bool CancelsUnreadRows => true;

    /// <inheritdoc />
    public override SqlStatement ToSql() => SqlRenderContext.Render(DialectRenderer.For<TDialect>(), new SelectNode(State));
}
