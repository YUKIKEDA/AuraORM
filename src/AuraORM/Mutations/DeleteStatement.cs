using System.Collections.Immutable;

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

    internal DeleteStatement(IStatementRunner? session, TableAlias target, TColumns table, SqlNode? where)
        : base(session)
    {
        _target = target;
        Table = table;
        _where = where;
    }

    /// <summary>
    /// Gets the columns of the table, which the lambda of <c>Returning</c> gets.
    /// </summary>
    internal TColumns Table { get; }

    internal override SqlNode CreateNode(ImmutableArray<SqlNode> returning) => new DeleteNode(_target, _where, returning);
}
