namespace AuraORM;

/// <content>
/// The string functions (<c>docs/design.ja.md</c> §4.2). The type parameter <c>T</c> is <see cref="string"/> or a nullable
/// <see cref="string"/>; its constraint keeps other types out and keeps the nullability of the operands in the result.
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Concatenates strings: <c>(a || b)</c> on SQLite and PostgreSQL, <c>CONCAT(a, b)</c> on MySQL and MariaDB, and
    /// <c>(a + b)</c> on SQL Server. On every dialect the result is null when any operand is null.
    /// </summary>
    /// <typeparam name="T">The string type of the operands and the result.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the operands.</typeparam>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <param name="rest">More strings.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Concat<T, TLevel>(IExpr<T, TLevel> first, IExpr<T, TLevel> second, params IExpr<T, TLevel>[] rest)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(rest);
        var nodes = new SqlNode[rest.Length + 2];
        nodes[0] = first.Node;
        nodes[1] = second.Node;
        for (int i = 0; i < rest.Length; i++)
        {
            nodes[i + 2] = rest[i].Node;
        }

        return Function<T, TLevel>(SqlFunction.Concat, nodes);
    }

    /// <inheritdoc cref="Concat{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel}, IExpr{T, TLevel}[])"/>
    public static IExpr<T, TLevel> Concat<T, TLevel>(IExpr<T, TLevel> first, string second)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Concat, first.Node, Param(second));

    /// <summary>
    /// Converts a string to lower case: <c>LOWER(a)</c>.
    /// </summary>
    /// <typeparam name="T">The string type.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Lower<T, TLevel>(IExpr<T, TLevel> value)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Lower, value.Node);

    /// <summary>
    /// Converts a string to upper case: <c>UPPER(a)</c>.
    /// </summary>
    /// <typeparam name="T">The string type.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Upper<T, TLevel>(IExpr<T, TLevel> value)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Upper, value.Node);

    /// <summary>
    /// Removes leading and trailing spaces: <c>TRIM(a)</c>.
    /// </summary>
    /// <typeparam name="T">The string type.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Trim<T, TLevel>(IExpr<T, TLevel> value)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Trim, value.Node);

    /// <summary>
    /// Returns part of a string: <c>SUBSTRING(a, start, length)</c>.
    /// </summary>
    /// <typeparam name="T">The string type.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <param name="start">The one-based position of the first character.</param>
    /// <param name="length">The number of characters.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Substring<T, TLevel>(IExpr<T, TLevel> value, int start, int length)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Substring, value.Node, Param(start), Param(length));

    /// <summary>
    /// Returns the number of characters of a string: <c>LENGTH(a)</c> on SQLite, <c>CHAR_LENGTH(a)</c> on PostgreSQL, MySQL,
    /// and MariaDB, and <c>LEN(a)</c> on SQL Server, which does not count trailing spaces.
    /// </summary>
    /// <typeparam name="T">The string type.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <returns>The expression. It is nullable, because the string may be null.</returns>
    public static IExpr<int?, TLevel> Length<T, TLevel>(IExpr<T, TLevel> value)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => Function<int?, TLevel>(SqlFunction.Length, value.Node);
}
