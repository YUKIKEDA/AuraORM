namespace AuraORM;

/// <summary>
/// A column of a table in the statement. It can be assigned in <c>UPDATE</c>.
/// </summary>
/// <typeparam name="T">The CLR type of the column, including whether it can be null.</typeparam>
public sealed class Column<T> : IExpr<T, PerRow>, ISqlNodeSource
{
    private readonly ColumnNode _node;

    internal Column(TableAlias table, string name)
    {
        _node = new ColumnNode(table, name);
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name => _node.Name;

    SqlNode IExpr<T, PerRow>.Node => _node;

    SqlNode ISqlNodeSource.SourceNode => _node;

    internal ColumnNode Node => _node;
}
