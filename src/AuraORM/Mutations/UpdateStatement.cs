using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>UPDATE</c> with its assignments and its condition (<c>docs/design.ja.md</c> §7).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
/// <remarks>
/// On MySQL and MariaDB, a value that reads a column assigned before it, and on MySQL, a subquery that reads the table
/// being updated, make <c>ToSql</c> and <c>ExecuteAsync</c> throw <see cref="InvalidOperationException"/> (§7, §14.3).
/// </remarks>
public sealed class UpdateStatement<TDialect, TMode, TColumns> : MutationStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly TableAlias _target;
    private readonly ImmutableArray<Assignment> _assignments;
    private readonly SqlNode? _where;

    internal UpdateStatement(IStatementRunner? session, TableAlias target, ImmutableArray<Assignment> assignments, SqlNode? where)
        : base(session)
    {
        _target = target;
        _assignments = assignments;
        _where = where;
    }

    private protected override SqlNode CreateNode() => new UpdateNode(_target, _assignments, _where);
}
