using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// Builds statements without a connection or a provider, for their SQL only (<c>docs/design.ja.md</c> §12.3, R9). It has the
/// same statement entry points as <see cref="Database{TDialect}"/>; the statements are <see cref="SqlOnly"/>, so they have
/// <c>ToSql()</c> and no run methods.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
public sealed class SqlBuilder<TDialect>
    where TDialect : IDialect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilder{TDialect}"/> class.
    /// </summary>
    public SqlBuilder()
    {
        _ = DialectRenderer.For<TDialect>();
    }

    /// <summary>
    /// Starts a SELECT statement: <c>SELECT … FROM t</c>. The lambdas that follow get the column set of the table or the
    /// named query (<c>docs/design.ja.md</c> §5.1, §5.11).
    /// </summary>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <param name="table">A table, such as <c>User.Table</c>, or a named query (<c>query.As("t")</c>).</param>
    /// <returns>The statement.</returns>
    public SelectFrom<TDialect, SqlOnly, TColumns, TNullable> From<TColumns, TNullable>(ITableSource<TColumns, TNullable> table)
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable> =>
        SelectStart.From<TDialect, SqlOnly, TColumns, TNullable>(session: null, table, ImmutableArray<NamedQueryDefinition>.Empty);

    /// <summary>
    /// Starts a SELECT statement with a <c>WITH</c>: the named queries are written as CTEs, in order, and a later one can use
    /// an earlier one (<c>docs/design.ja.md</c> §5.11).
    /// </summary>
    /// <param name="queries">The named queries, each itself and not an aliased reference.</param>
    /// <returns>The start of the statement.</returns>
    /// <exception cref="ArgumentException">A query is an aliased reference, or two have the same name.</exception>
    public SelectWith<TDialect, SqlOnly> With(params NamedQuery[] queries) => new(session: null, SelectStart.Ctes(queries));
}
