using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// One <c>DROP INDEX</c> per <see cref="IndexAttribute"/> of the table (design §13.4). The unique constraints of
/// <see cref="UniqueAttribute"/> are part of the table and are not dropped.
/// </summary>
/// <typeparam name="TDialect">The dialect the statements are written for.</typeparam>
/// <typeparam name="TMode">Whether the statements can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public sealed class DropIndexesStatement<TDialect, TMode> : IndexDdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private readonly bool _ifExists;

    internal DropIndexesStatement(IStatementRunner? session, TableInfo table, bool ifExists)
        : base(session, table)
    {
        _ifExists = ifExists;
    }

    internal DropIndexesStatement<TDialect, TMode> WithIfExists() => new(Session, Table, ifExists: true);

    internal override ImmutableArray<SqlStatement> RenderAll() => Renderer.DropIndexes(Table, _ifExists);
}
