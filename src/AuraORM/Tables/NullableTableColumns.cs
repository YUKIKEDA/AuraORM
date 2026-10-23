using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// The base of a generated <c>NullableColumns</c> class: the columns of a table on the nullable side of an outer join.
/// Selected as a whole row, it reads <see langword="null"/> when the join found no row (<c>docs/design.ja.md</c> §5.3).
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public abstract class NullableTableColumns<TRow> : IProjection<TRow?, PerRow>
    where TRow : class
{
    private ITableColumn[]? _columns;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableTableColumns{TRow}"/> class.
    /// </summary>
    protected NullableTableColumns() { }

    /// <summary>
    /// Returns the columns in declaration order.
    /// </summary>
    /// <returns>The columns.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract ITableColumn[] GetColumns();

    /// <summary>
    /// Reads the row, or <see langword="null"/> when every primary key column is null (every column, for a table without a
    /// primary key).
    /// </summary>
    /// <param name="reader">The reader of the current row.</param>
    /// <returns>The row, or <see langword="null"/>.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract TRow? ReadRow(ref RowReader reader);

    void IProjection<TRow?, PerRow>.CollectSelectItems(List<SqlNode> items)
    {
        foreach (ITableColumn column in _columns ??= GetColumns())
        {
            items.Add(column.ColumnNode);
        }
    }

    TRow? IProjection<TRow?, PerRow>.Read(ref RowReader reader) => ReadRow(ref reader);
}
