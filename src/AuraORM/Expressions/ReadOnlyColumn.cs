namespace AuraORM;

/// <summary>
/// A column that cannot be assigned in <c>UPDATE</c>: an <c>[AutoIncrement]</c> column (<c>docs/design.ja.md</c> §3.4).
/// </summary>
/// <typeparam name="T">The CLR type of the column.</typeparam>
public sealed class ReadOnlyColumn<T> : IExpr<T, PerRow>, ISqlNodeSource
{
    private readonly ColumnNode _node;

    internal ReadOnlyColumn(TableAlias table, string name)
    {
        _node = new ColumnNode(table, name);
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name => _node.Name;

    SqlNode IExpr<T, PerRow>.Node => _node;

    SqlNode ISqlNodeSource.SourceNode => _node;
}
