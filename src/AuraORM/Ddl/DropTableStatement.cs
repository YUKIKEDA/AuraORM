namespace AuraORM;

/// <summary>
/// <c>DROP TABLE</c> (design §13.8).
/// </summary>
/// <typeparam name="TDialect">The dialect the statement is written for.</typeparam>
/// <typeparam name="TMode">Whether the statement can run (<see cref="Executable"/>) or only render (<see cref="SqlOnly"/>).</typeparam>
public sealed class DropTableStatement<TDialect, TMode> : TableDdlStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private readonly bool _ifExists;

    internal DropTableStatement(IStatementRunner? session, TableInfo table, bool ifExists)
        : base(session, table)
    {
        _ifExists = ifExists;
    }

    internal DropTableStatement<TDialect, TMode> WithIfExists() => new(Session, Table, ifExists: true);

    internal override SqlStatement Render() => Renderer.DropTable(Table, _ifExists);
}
