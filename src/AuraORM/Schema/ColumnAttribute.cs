namespace AuraORM;

/// <summary>
/// Sets the name of a column.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnAttribute"/> class.
    /// </summary>
    /// <param name="name">The column name, used as written.</param>
    public ColumnAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name { get; }
}
