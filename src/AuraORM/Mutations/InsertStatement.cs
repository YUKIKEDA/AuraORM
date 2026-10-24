using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>INSERT</c> with its rows (<c>docs/design.ja.md</c> §6.2).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
public sealed class InsertStatement<TDialect, TMode, TColumns> : MutationStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly TableAlias _target;
    private readonly ImmutableArray<string> _columns;
    private readonly ImmutableArray<ImmutableArray<ParameterNode>> _rows;

    internal InsertStatement(
        IStatementRunner? session,
        TableAlias target,
        TColumns table,
        ImmutableArray<string> columns,
        ImmutableArray<ImmutableArray<ParameterNode>> rows
    )
        : base(session)
    {
        _target = target;
        Table = table;
        _columns = columns;
        _rows = rows;
    }

    /// <summary>
    /// Gets the columns of the table, which the lambda of <c>Returning</c> gets.
    /// </summary>
    internal TColumns Table { get; }

    internal override SqlNode CreateNode(ImmutableArray<SqlNode> returning) => new InsertNode(_target, _columns, _rows, returning);
}
