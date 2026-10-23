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
    /// Starts a SELECT statement: <c>FROM table</c> (<c>docs/design.ja.md</c> §5). The lambdas of the statement get the
    /// table's column set.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <typeparam name="TInsert">The <c>INSERT</c> model.</typeparam>
    /// <param name="table">The table reference, such as <c>User.Table</c>.</param>
    /// <returns>The statement.</returns>
    public SelectFrom<TDialect, SqlOnly, TColumns, TNullable> From<TRow, TColumns, TNullable, TInsert>(
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => SelectStart.From<TDialect, SqlOnly, TRow, TColumns, TNullable, TInsert>(session: null, table);
}
