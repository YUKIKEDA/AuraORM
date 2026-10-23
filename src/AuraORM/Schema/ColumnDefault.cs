namespace AuraORM;

/// <summary>
/// The default value of a column in the database, from <see cref="DefaultAttribute"/> or <see cref="DefaultSqlAttribute"/>.
/// </summary>
public sealed class ColumnDefault
{
    private ColumnDefault(SqlDefault? named, object? constant, string? sql)
    {
        Named = named;
        Constant = constant;
        Sql = sql;
    }

    /// <summary>
    /// Gets the default <c>CURRENT_TIMESTAMP</c>.
    /// </summary>
    public static ColumnDefault CurrentTimestamp { get; } = new(SqlDefault.CurrentTimestamp, null, null);

    /// <summary>
    /// Gets the named default, or <see langword="null"/>.
    /// </summary>
    public SqlDefault? Named { get; }

    /// <summary>
    /// Gets the constant default, or <see langword="null"/>.
    /// </summary>
    public object? Constant { get; }

    /// <summary>
    /// Gets the default written in SQL, or <see langword="null"/>.
    /// </summary>
    public string? Sql { get; }

    /// <summary>
    /// Creates a constant default.
    /// </summary>
    /// <param name="value">A number, a string, or a Boolean.</param>
    /// <returns>The default.</returns>
    public static ColumnDefault FromConstant(object value) => new(null, value, null);

    /// <summary>
    /// Creates a default written in SQL.
    /// </summary>
    /// <param name="sql">The SQL text.</param>
    /// <returns>The default.</returns>
    public static ColumnDefault FromSql(string sql) => new(null, null, sql);
}
