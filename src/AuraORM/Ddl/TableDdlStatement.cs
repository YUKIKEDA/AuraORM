using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// A DDL statement on one table: <c>CREATE TABLE</c> or <c>DROP TABLE</c> (design §13).
/// </summary>
/// <typeparam name="TDialect">The dialect the statement is written for.</typeparam>
/// <typeparam name="TMode">Whether the statement can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public abstract class TableDdlStatement<TDialect, TMode> : DdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected TableDdlStatement(IStatementRunner? session, TableInfo table)
        : base(session, table) { }

    /// <summary>
    /// Renders the statement without a connection.
    /// </summary>
    /// <returns>The SQL of the statement. DDL has no parameters.</returns>
    /// <exception cref="InvalidOperationException">
    /// A name, a type, or a key is beyond the dialect's limit, or the table declares what the dialect cannot write
    /// (design §13, §14.3).
    /// </exception>
    public SqlStatement ToSql() => Render();

    internal abstract SqlStatement Render();

    internal sealed override ImmutableArray<SqlStatement> RenderAll() => [Render()];
}
