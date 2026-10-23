namespace AuraORM;

/// <summary>
/// What the schema declares about one column. The source generator creates the instances.
/// </summary>
public sealed class ColumnInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnInfo"/> class.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="type">The CLR type of the column.</param>
    public ColumnInfo(string name, ColumnType type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the CLR type of the column.
    /// </summary>
    public ColumnType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the column allows <c>NULL</c>.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is part of the primary key.
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether the database assigns the values of the column.
    /// </summary>
    public bool IsAutoIncrement { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column has a unique constraint of its own.
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Gets the maximum length of a string or byte array column, or <see langword="null"/> for no limit.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the precision of a decimal column, or <see langword="null"/> for the default.
    /// </summary>
    public int? Precision { get; init; }

    /// <summary>
    /// Gets the scale of a decimal column, or <see langword="null"/> for the default.
    /// </summary>
    public int? Scale { get; init; }

    /// <summary>
    /// Gets the default value in the database, or <see langword="null"/>.
    /// </summary>
    public ColumnDefault? Default { get; init; }

    /// <summary>
    /// Gets the column that this column refers to, or <see langword="null"/>.
    /// </summary>
    public ForeignKeyInfo? References { get; init; }
}
