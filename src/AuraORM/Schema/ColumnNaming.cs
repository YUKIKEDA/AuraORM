namespace AuraORM;

/// <summary>
/// How a column name is made from a member name when the member has no <see cref="ColumnAttribute"/>
/// (<c>docs/design.ja.md</c> §3.2).
/// </summary>
public enum ColumnNaming
{
    /// <summary>
    /// The member name as written: <c>CreatedAt</c>.
    /// </summary>
    AsIs,

    /// <summary>
    /// The member name in lower snake case: <c>created_at</c>, <c>http_code</c>.
    /// </summary>
    SnakeCase,
}
