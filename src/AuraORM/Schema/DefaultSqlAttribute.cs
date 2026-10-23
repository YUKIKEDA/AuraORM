namespace AuraORM;

/// <summary>
/// Gives a column a default value written in SQL. The text goes into DDL unchanged. The column can then be left out of
/// <c>Insert</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class DefaultSqlAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSqlAttribute"/> class.
    /// </summary>
    /// <param name="sql">The SQL of the default value.</param>
    public DefaultSqlAttribute(string sql)
    {
        Sql = sql;
    }

    /// <summary>
    /// Gets the SQL of the default value.
    /// </summary>
    public string Sql { get; }
}
