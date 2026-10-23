namespace AuraORM;

/// <summary>
/// A table reference: <c>User.Table</c>. Statements start from it (<c>db.From(User.Table)</c>), and its type arguments
/// give the lambdas of the statement their column sets (<c>docs/design.ja.md</c> §3.4).
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
/// <typeparam name="TColumns">The column set.</typeparam>
/// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
/// <typeparam name="TInsert">The <c>INSERT</c> model.</typeparam>
public sealed class Table<TRow, TColumns, TNullable, TInsert> : ITableReference
    where TRow : class
    where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
    where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
    where TInsert : InsertModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Table{TRow, TColumns, TNullable, TInsert}"/> class. The source generator
    /// creates the one instance of each table.
    /// </summary>
    /// <param name="info">What the schema declares about the table.</param>
    public Table(TableInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        Info = info;
    }

    /// <summary>
    /// Gets what the schema declares about the table.
    /// </summary>
    public TableInfo Info { get; }
}
