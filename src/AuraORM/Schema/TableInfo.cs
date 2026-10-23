namespace AuraORM;

/// <summary>
/// What the schema declares about a table. The source generator creates the instances.
/// </summary>
public sealed class TableInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableInfo"/> class.
    /// </summary>
    /// <param name="name">The table name.</param>
    /// <param name="schema">The schema, or <see langword="null"/> for the connection's default.</param>
    /// <param name="columns">The columns in declaration order.</param>
    /// <param name="indexes">The indexes.</param>
    public TableInfo(string name, string? schema, IReadOnlyList<ColumnInfo> columns, IReadOnlyList<IndexInfo> indexes)
    {
        Name = name;
        Schema = schema;
        Columns = columns;
        Indexes = indexes;
    }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the schema, or <see langword="null"/> for the connection's default.
    /// </summary>
    public string? Schema { get; }

    /// <summary>
    /// Gets the columns in declaration order.
    /// </summary>
    public IReadOnlyList<ColumnInfo> Columns { get; }

    /// <summary>
    /// Gets the indexes.
    /// </summary>
    public IReadOnlyList<IndexInfo> Indexes { get; }
}
