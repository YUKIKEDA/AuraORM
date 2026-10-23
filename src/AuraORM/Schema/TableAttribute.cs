namespace AuraORM;

/// <summary>
/// Declares a table. The annotated partial record or class is the row type, and the source generator adds
/// <c>Table</c>, <c>Columns</c>, <c>NullableColumns</c>, and <c>Insert</c> to it (<c>docs/design.ja.md</c> §3).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableAttribute"/> class.
    /// </summary>
    /// <param name="name">The table name, used as written.</param>
    public TableAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the schema that holds the table, or <see langword="null"/> for the connection's default.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets how the names of columns without <see cref="ColumnAttribute"/> are made from member names.
    /// </summary>
    public ColumnNaming Naming { get; set; }
}
