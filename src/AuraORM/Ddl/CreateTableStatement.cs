namespace AuraORM;

/// <summary>
/// <c>CREATE TABLE</c>: the columns in declaration order, then the primary key, the unique constraints, and the foreign
/// keys (design §13.2).
/// </summary>
/// <typeparam name="TDialect">The dialect the statement is written for.</typeparam>
/// <typeparam name="TMode">Whether the statement can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public sealed class CreateTableStatement<TDialect, TMode> : TableDdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private readonly bool _ifNotExists;

    internal CreateTableStatement(IStatementRunner? session, TableInfo table, bool ifNotExists)
        : base(session, table)
    {
        _ifNotExists = ifNotExists;
    }

    internal CreateTableStatement<TDialect, TMode> WithIfNotExists() => new(Session, Table, ifNotExists: true);

    internal override SqlStatement Render() => Renderer.CreateTable(Table, _ifNotExists);
}
