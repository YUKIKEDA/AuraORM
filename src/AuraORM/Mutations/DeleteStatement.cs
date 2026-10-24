namespace AuraORM;

/// <summary>
/// A <c>DELETE</c> with its condition (<c>docs/design.ja.md</c> §8).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
/// <remarks>
/// On MySQL, a subquery that reads the table being deleted from makes <c>ToSql</c> and <c>ExecuteAsync</c> throw
/// <see cref="InvalidOperationException"/> (§7, §8, §14.3).
/// </remarks>
public sealed class DeleteStatement<TDialect, TMode, TColumns> : MutationStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly TableAlias _target;
    private readonly SqlNode? _where;

    internal DeleteStatement(IStatementRunner? session, TableAlias target, SqlNode? where)
        : base(session)
    {
        _target = target;
        _where = where;
    }

    private protected override SqlNode CreateNode() => new DeleteNode(_target, _where);
}
