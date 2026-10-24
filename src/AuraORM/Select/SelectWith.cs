using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// The start of a SELECT statement with a <c>WITH</c> (<c>docs/design.ja.md</c> §5.11): the named queries listed in
/// <c>With</c> are written as CTEs, in order, and every reference to them in the statement writes their name.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
public sealed class SelectWith<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private readonly IStatementRunner? _session;
    private readonly ImmutableArray<NamedQueryDefinition> _ctes;

    internal SelectWith(IStatementRunner? session, ImmutableArray<NamedQueryDefinition> ctes)
    {
        _session = session;
        _ctes = ctes;
    }

    /// <summary>
    /// Starts the statement: <c>WITH … SELECT … FROM t</c>. The lambdas that follow get the column set of the table or the
    /// named query.
    /// </summary>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <param name="table">A table, such as <c>User.Table</c>, or a named query.</param>
    /// <returns>The statement.</returns>
    public SelectFrom<TDialect, TMode, TColumns, TNullable> From<TColumns, TNullable>(ITableSource<TColumns, TNullable> table)
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable> => SelectStart.From<TDialect, TMode, TColumns, TNullable>(_session, table, _ctes);
}
