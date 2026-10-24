namespace AuraORM;

/// <summary>
/// The <c>CREATE INDEX</c> or <c>DROP INDEX</c> statements of a table's <see cref="IndexAttribute"/> indexes, one per index
/// in the order they are declared (design §13.4).
/// </summary>
/// <typeparam name="TDialect">The dialect the statements are written for.</typeparam>
/// <typeparam name="TMode">Whether the statements can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public abstract class IndexDdlStatement<TDialect, TMode> : DdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected IndexDdlStatement(IStatementRunner? session, TableInfo table)
        : base(session, table) { }

    /// <summary>
    /// Renders the statements without a connection.
    /// </summary>
    /// <returns>One statement per index, in the order the indexes are declared; empty when the table has none.</returns>
    /// <exception cref="InvalidOperationException">A name or a key is beyond the dialect's limit (design §13.7).</exception>
    public IReadOnlyList<SqlStatement> ToSql() => RenderAll();
}
