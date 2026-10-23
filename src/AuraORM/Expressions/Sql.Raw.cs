namespace AuraORM;

/// <content>
/// Raw SQL fragments (<c>docs/design.ja.md</c> §11.1). AuraORM does not parse the SQL inside, so the type and the level that
/// the caller declares are not checked.
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Writes a row-level condition in SQL: <c>Raw($"{u.Name} COLLATE NOCASE = {name}")</c>.
    /// </summary>
    /// <param name="sql">The fragment. Values in holes become parameters.</param>
    /// <returns>The condition.</returns>
    public static Condition<PerRow> Raw(SqlInterpolatedStringHandler sql) => new(sql.ToNode());

    /// <summary>
    /// Writes a row-level expression in SQL: <c>Raw&lt;int&gt;($"instr({u.Name}, {"a"})")</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type that the value is read as.</typeparam>
    /// <param name="sql">The fragment. Values in holes become parameters.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, PerRow> Raw<T>(SqlInterpolatedStringHandler sql) => new SqlExpr<T, PerRow>(sql.ToNode());

    /// <summary>
    /// Writes a group-level expression in SQL, such as an aggregate that <see cref="Sql"/> does not have.
    /// </summary>
    /// <typeparam name="T">The CLR type that the value is read as.</typeparam>
    /// <param name="sql">The fragment. Values in holes become parameters.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, PerGroup> RawAggregate<T>(SqlInterpolatedStringHandler sql) => new SqlExpr<T, PerGroup>(sql.ToNode());

    /// <summary>
    /// Writes a group-level condition in SQL, for <c>HAVING</c>.
    /// </summary>
    /// <param name="sql">The fragment. Values in holes become parameters.</param>
    /// <returns>The condition.</returns>
    public static Condition<PerGroup> RawHaving(SqlInterpolatedStringHandler sql) => new(sql.ToNode());

    /// <summary>
    /// Puts text into a raw fragment without checking it. Use it only for SQL that the program itself chooses, never for
    /// input: the text reaches the database as SQL.
    /// </summary>
    /// <param name="sql">The SQL text.</param>
    /// <returns>The fragment, for a hole of a raw fragment.</returns>
    public static SqlFragment Unsafe(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        return new SqlFragment(new LiteralNode(sql));
    }

    /// <summary>
    /// Puts an identifier chosen at run time into a raw fragment, quoted with the dialect's quote.
    /// </summary>
    /// <param name="name">The identifier. A quote inside it is doubled.</param>
    /// <returns>The fragment, for a hole of a raw fragment.</returns>
    public static SqlFragment Identifier(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new SqlFragment(new IdentifierNode(null, name));
    }
}
