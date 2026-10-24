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

    /// <summary>
    /// Gets the table, for the clauses that follow.
    /// </summary>
    internal TableAlias Target => _target;

    /// <summary>
    /// Gets a value indicating whether the rows set no column (§6.2), which SQLite's upsert does not accept (§10.1).
    /// </summary>
    internal bool SetsNoColumn => _columns.IsEmpty;

    internal override SqlNode CreateNode(ImmutableArray<SqlNode> returning) => CreateNode(returning, null);

    /// <summary>
    /// Creates the node of the statement with the clause that handles a conflict.
    /// </summary>
    /// <param name="returning">The items of <c>Returning</c>, or an empty array.</param>
    /// <param name="upsert">The clause, or <see langword="null"/>.</param>
    /// <returns>The node.</returns>
    internal SqlNode CreateNode(ImmutableArray<SqlNode> returning, UpsertClause? upsert) =>
        new InsertNode(_target, _columns, _rows, returning, upsert);
}
