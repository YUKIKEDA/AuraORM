namespace AuraORM;

/// <summary>
/// A column of the nullable side of an outer join (<c>docs/design.ja.md</c> §5.3). It is an expression that may be null,
/// and it cannot be assigned.
/// </summary>
/// <typeparam name="T">The nullable CLR type of the column.</typeparam>
public sealed class NullableColumn<T> : IExpr<T, PerRow>, ITableColumn, ISqlNodeSource
{
    private readonly ColumnNode _node;

    internal NullableColumn(TableAlias table, string name)
    {
        _node = new ColumnNode(table, name);
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name => _node.Name;

    SqlNode IExpr<T, PerRow>.Node => _node;

    ColumnNode ITableColumn.ColumnNode => _node;

    SqlNode ISqlNodeSource.SourceNode => _node;
}
