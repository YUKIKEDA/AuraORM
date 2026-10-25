using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// One <c>CREATE INDEX</c> (or <c>CREATE UNIQUE INDEX</c>) per <see cref="IndexAttribute"/> of the table (design §13.4).
/// </summary>
/// <typeparam name="TDialect">The dialect the statements are written for.</typeparam>
/// <typeparam name="TMode">Whether the statements can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public sealed class CreateIndexesStatement<TDialect, TMode> : IndexDdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private readonly bool _ifNotExists;

    internal CreateIndexesStatement(IStatementRunner? session, TableInfo table, bool ifNotExists)
        : base(session, table)
    {
        _ifNotExists = ifNotExists;
    }

    internal CreateIndexesStatement<TDialect, TMode> WithIfNotExists() => new(Session, Table, ifNotExists: true);

    internal override ImmutableArray<SqlStatement> RenderAll() => Renderer.CreateIndexes(Table, _ifNotExists);
}
