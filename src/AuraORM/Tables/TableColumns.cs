using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// The base of a generated <c>Columns</c> class: the columns of one occurrence of a table, which is also the projection of
/// the whole row (<c>Select(u =&gt; u)</c>, <c>docs/design.ja.md</c> §3.4).
/// </summary>
/// <typeparam name="TRow">The row type.</typeparam>
public abstract class TableColumns<TRow> : IProjection<TRow, PerRow>
    where TRow : class
{
    private ITableColumn[]? _columns;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumns{TRow}"/> class.
    /// </summary>
    protected TableColumns() { }

    /// <summary>
    /// Returns the columns in declaration order.
    /// </summary>
    /// <returns>The columns.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract ITableColumn[] GetColumns();

    /// <summary>
    /// Reads the row from the columns returned by <see cref="GetColumns"/>, in the same order.
    /// </summary>
    /// <param name="reader">The reader of the current row.</param>
    /// <returns>The row.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract TRow ReadRow(ref RowReader reader);

    void IProjection<TRow, PerRow>.CollectSelectItems(List<SqlNode> items)
    {
        foreach (ITableColumn column in _columns ??= GetColumns())
        {
            items.Add(column.ColumnNode);
        }
    }

    TRow IProjection<TRow, PerRow>.Read(ref RowReader reader) => ReadRow(ref reader);
}
