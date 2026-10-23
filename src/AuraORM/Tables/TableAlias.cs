using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// One occurrence of a table in a statement. Columns are created from it, so that a column is always bound to a table that
/// the statement contains (<c>docs/design.ja.md</c> §3.4).
/// </summary>
/// <remarks>
/// Only AuraORM creates instances. The members are used by the code that the source generator emits.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TableAlias
{
    internal TableAlias(string? schema, string table, string? alias)
    {
        Schema = schema;
        Table = table;
        Alias = alias;
    }

    internal string? Schema { get; }

    internal string Table { get; }

    internal string? Alias { get; }

    /// <summary>
    /// Gets the name that qualifies the columns: the alias if there is one, otherwise the table name.
    /// </summary>
    internal string Reference => Alias ?? Table;

    /// <summary>
    /// Creates a column that can be assigned in <c>UPDATE</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type of the column, including whether it can be null.</typeparam>
    /// <param name="name">The column name.</param>
    /// <returns>The column.</returns>
    public Column<T> Column<T>(string name) => new(this, name);

    /// <summary>
    /// Creates a column that cannot be assigned (<c>[AutoIncrement]</c>).
    /// </summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <param name="name">The column name.</param>
    /// <returns>The column.</returns>
    public ReadOnlyColumn<T> ReadOnlyColumn<T>(string name) => new(this, name);

    /// <summary>
    /// Creates a column of the nullable side of an outer join. It is an expression, not a column.
    /// </summary>
    /// <typeparam name="T">The nullable CLR type of the column.</typeparam>
    /// <param name="name">The column name.</param>
    /// <returns>The expression.</returns>
    public IExpr<T, PerRow> NullableColumn<T>(string name) => new SqlExpr<T, PerRow>(new ColumnNode(this, name));
}
