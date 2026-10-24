using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// A DDL statement of design §13: <c>CREATE TABLE</c>, <c>DROP TABLE</c>, or the <c>CREATE INDEX</c> / <c>DROP INDEX</c>
/// statements of a table's <see cref="IndexAttribute"/> indexes.
/// </summary>
/// <typeparam name="TDialect">The dialect the statement is written for.</typeparam>
/// <typeparam name="TMode">Whether the statement can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public abstract class DdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private protected DdlStatement(IStatementRunner? session, TableInfo table)
    {
        Session = session;
        Table = table;
    }

    internal IStatementRunner? Session { get; }

    internal TableInfo Table { get; }

    internal static DdlRenderer Renderer => DialectRenderer.For<TDialect>().Ddl;

    // The statements in the order they run. Rendering checks the limits of §13.7, so it throws before anything runs.
    internal abstract ImmutableArray<SqlStatement> RenderAll();
}
