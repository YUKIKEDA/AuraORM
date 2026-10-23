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

    private Table(TableInfo info, string alias)
    {
        Info = info;
        Alias = alias;
    }

    /// <summary>
    /// Gets what the schema declares about the table.
    /// </summary>
    public TableInfo Info { get; }

    /// <summary>
    /// Gets the alias of this reference, or <see langword="null"/> when it has none.
    /// </summary>
    internal string? Alias { get; }

    /// <inheritdoc/>
    string? ITableReference.Alias => Alias;

    /// <summary>
    /// Gives the table an alias in a statement: <c>FROM "users" AS "u"</c>, and its columns are written <c>"u"."column"</c>
    /// (<c>docs/design.ja.md</c> §5.2). A table used twice in one statement (a self-join) needs an alias for the second use.
    /// </summary>
    /// <param name="alias">The alias.</param>
    /// <returns>The table reference with the alias.</returns>
    /// <exception cref="ArgumentException"><paramref name="alias"/> is empty.</exception>
    public Table<TRow, TColumns, TNullable, TInsert> As(string alias)
    {
        ArgumentException.ThrowIfNullOrEmpty(alias);
        return new Table<TRow, TColumns, TNullable, TInsert>(Info, alias);
    }
}
