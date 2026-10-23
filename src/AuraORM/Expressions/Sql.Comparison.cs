namespace AuraORM;

/// <summary>
/// The SQL functions. Write <c>using static AuraORM.Sql;</c> and call them as <c>Eq(u.Age, 20)</c>
/// (<c>docs/design.ja.md</c> §4.2).
/// </summary>
/// <remarks>
/// A value argument (of type <c>T</c>) is bound as a parameter. An expression argument is written as SQL. The two operands of
/// a function have the same evaluation level. Every function here works on every dialect.
/// </remarks>
public static partial class Sql
{
    /// <summary>
    /// Compares two expressions for equality: <c>a = b</c>.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition. It is not true when either operand is <c>NULL</c>; use <see cref="IsNull{T, TLevel}(IExpr{T, TLevel})"/> or
    /// <see cref="IsNotDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/> for nulls.</returns>
    public static Condition<TLevel> Eq<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.Equal, left.Node, right.Node);

    /// <inheritdoc cref="Eq{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Eq<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.Equal, left.Node, right.Node);

    /// <inheritdoc cref="Eq{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Eq<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.Equal, left.Node, right.Node);

    /// <inheritdoc cref="Eq{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Eq<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.Equal, left.Node, Param(right));

    /// <summary>
    /// Compares two expressions for inequality: <c>a &lt;&gt; b</c>.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition. It is not true when either operand is <c>NULL</c>.</returns>
    public static Condition<TLevel> Ne<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.NotEqual, left.Node, right.Node);

    /// <inheritdoc cref="Ne{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Ne<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.NotEqual, left.Node, right.Node);

    /// <inheritdoc cref="Ne{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Ne<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.NotEqual, left.Node, right.Node);

    /// <inheritdoc cref="Ne{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Ne<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => Compare<TLevel>(ComparisonOperator.NotEqual, left.Node, Param(right));

    /// <summary>
    /// Tests whether the left operand is greater: <c>a &gt; b</c>.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Gt<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThan, left.Node, right.Node);

    /// <inheritdoc cref="Gt{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Gt<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThan, left.Node, right.Node);

    /// <inheritdoc cref="Gt{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Gt<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThan, left.Node, right.Node);

    /// <inheritdoc cref="Gt{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Gt<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThan, left.Node, Param(right));

    /// <summary>
    /// Tests whether the left operand is greater or equal: <c>a &gt;= b</c>.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Ge<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThanOrEqual, left.Node, right.Node);

    /// <inheritdoc cref="Ge{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Ge<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThanOrEqual, left.Node, right.Node);

    /// <inheritdoc cref="Ge{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Ge<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThanOrEqual, left.Node, right.Node);

    /// <inheritdoc cref="Ge{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Ge<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.GreaterThanOrEqual, left.Node, Param(right));

    /// <summary>
    /// Tests whether the left operand is less: <c>a &lt; b</c>.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Lt<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThan, left.Node, right.Node);

    /// <inheritdoc cref="Lt{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Lt<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThan, left.Node, right.Node);

    /// <inheritdoc cref="Lt{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Lt<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThan, left.Node, right.Node);

    /// <inheritdoc cref="Lt{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Lt<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThan, left.Node, Param(right));

    /// <summary>
    /// Tests whether the left operand is less or equal: <c>a &lt;= b</c>.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Le<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThanOrEqual, left.Node, right.Node);

    /// <inheritdoc cref="Le{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Le<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThanOrEqual, left.Node, right.Node);

    /// <inheritdoc cref="Le{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Le<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThanOrEqual, left.Node, right.Node);

    /// <inheritdoc cref="Le{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> Le<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => Ordered<T, TLevel>(ComparisonOperator.LessThanOrEqual, left.Node, Param(right));

    /// <summary>
    /// Tests whether a value is in a range, bounds included: <c>a BETWEEN low AND high</c>.
    /// </summary>
    /// <typeparam name="T">The type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the operands.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <param name="low">The lower bound.</param>
    /// <param name="high">The upper bound.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Between<T, TLevel>(IExpr<T, TLevel> value, T low, T high)
        where TLevel : EvaluationLevel => new(DecimalOrderingNode.For<T>(new BetweenNode(value.Node, Param(low), Param(high)), "BETWEEN"));

    /// <inheritdoc cref="Between{T, TLevel}(IExpr{T, TLevel}, T, T)"/>
    public static Condition<TLevel> Between<T, TLevel>(IExpr<T, TLevel> value, IExpr<T, TLevel> low, IExpr<T, TLevel> high)
        where TLevel : EvaluationLevel => new(DecimalOrderingNode.For<T>(new BetweenNode(value.Node, low.Node, high.Node), "BETWEEN"));

    /// <summary>
    /// Tests whether a value is one of a list of values: <c>a IN (@p0, @p1, …)</c>. An empty list is written <c>1 = 0</c>.
    /// </summary>
    /// <typeparam name="T">The type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <param name="values">The values. Each one becomes a parameter, so the SQL text depends on how many there are.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> In<T, TLevel>(IExpr<T, TLevel> value, IReadOnlyList<T> values)
        where TLevel : EvaluationLevel => new(new InListNode(value.Node, Params(values), negated: false));

    /// <inheritdoc cref="In{T, TLevel}(IExpr{T, TLevel}, IReadOnlyList{T})"/>
    public static Condition<TLevel> In<T, TLevel>(IExpr<T?, TLevel> value, IReadOnlyList<T> values)
        where T : struct
        where TLevel : EvaluationLevel => new(new InListNode(value.Node, Params(values), negated: false));

    /// <summary>
    /// Tests whether a value is none of a list of values: <c>a NOT IN (@p0, @p1, …)</c>. An empty list is written <c>1 = 1</c>.
    /// </summary>
    /// <typeparam name="T">The type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <param name="values">The values. Each one becomes a parameter.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> NotIn<T, TLevel>(IExpr<T, TLevel> value, IReadOnlyList<T> values)
        where TLevel : EvaluationLevel => new(new InListNode(value.Node, Params(values), negated: true));

    /// <inheritdoc cref="NotIn{T, TLevel}(IExpr{T, TLevel}, IReadOnlyList{T})"/>
    public static Condition<TLevel> NotIn<T, TLevel>(IExpr<T?, TLevel> value, IReadOnlyList<T> values)
        where T : struct
        where TLevel : EvaluationLevel => new(new InListNode(value.Node, Params(values), negated: true));

    /// <summary>
    /// Matches a string against a pattern: <c>a LIKE @p0</c>. Whether case matters depends on the database's collation.
    /// </summary>
    /// <typeparam name="T">The string type: <see cref="string"/> or a nullable <see cref="string"/>.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The matched string.</param>
    /// <param name="pattern">The pattern, with <c>%</c> and <c>_</c> as written in SQL.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Like<T, TLevel>(IExpr<T, TLevel> value, string pattern)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => new(new LikeNode(value.Node, Param(pattern), negated: false));

    /// <inheritdoc cref="Like{T, TLevel}(IExpr{T, TLevel}, string)"/>
    public static Condition<TLevel> Like<T, TLevel>(IExpr<T, TLevel> value, IExpr<T, TLevel> pattern)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => new(new LikeNode(value.Node, pattern.Node, negated: false));

    /// <summary>
    /// Tests that a string does not match a pattern: <c>a NOT LIKE @p0</c>.
    /// </summary>
    /// <typeparam name="T">The string type: <see cref="string"/> or a nullable <see cref="string"/>.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The matched string.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> NotLike<T, TLevel>(IExpr<T, TLevel> value, string pattern)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => new(new LikeNode(value.Node, Param(pattern), negated: true));

    /// <inheritdoc cref="NotLike{T, TLevel}(IExpr{T, TLevel}, string)"/>
    public static Condition<TLevel> NotLike<T, TLevel>(IExpr<T, TLevel> value, IExpr<T, TLevel> pattern)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => new(new LikeNode(value.Node, pattern.Node, negated: true));

    /// <summary>
    /// Tests that a string matches a pattern in which <paramref name="escape"/> makes the next <c>%</c> or <c>_</c> a plain
    /// character: <c>a LIKE @p0 ESCAPE @p1</c>.
    /// </summary>
    /// <typeparam name="T">The string type, <see cref="string"/> or <see cref="string"/>?.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <param name="pattern">The pattern, bound as a parameter.</param>
    /// <param name="escape">The escape character, bound as a one-character string parameter.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Like<T, TLevel>(IExpr<T, TLevel> value, string pattern, char escape)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => new(new LikeNode(value.Node, Param(pattern), negated: false, Param(escape.ToString())));

    /// <summary>
    /// Tests that a string does not match a pattern in which <paramref name="escape"/> makes the next <c>%</c> or <c>_</c> a
    /// plain character: <c>a NOT LIKE @p0 ESCAPE @p1</c>.
    /// </summary>
    /// <typeparam name="T">The string type, <see cref="string"/> or <see cref="string"/>?.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The string.</param>
    /// <param name="pattern">The pattern, bound as a parameter.</param>
    /// <param name="escape">The escape character, bound as a one-character string parameter.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> NotLike<T, TLevel>(IExpr<T, TLevel> value, string pattern, char escape)
        where T : IEnumerable<char>?
        where TLevel : EvaluationLevel => new(new LikeNode(value.Node, Param(pattern), negated: true, Param(escape.ToString())));

    private static Condition<TLevel> Compare<TLevel>(ComparisonOperator op, SqlNode left, SqlNode right)
        where TLevel : EvaluationLevel => new(new ComparisonNode(op, left, right));

    // A comparison by size of decimals is rejected on SQLite (§3.3).
    private static Condition<TLevel> Ordered<T, TLevel>(ComparisonOperator op, SqlNode left, SqlNode right)
        where TLevel : EvaluationLevel => new(DecimalOrderingNode.For<T>(new ComparisonNode(op, left, right), "a comparison by size"));

    private static ParameterNode<T> Param<T>(T value) => new(value);

    private static SqlNode[] Params<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var nodes = new SqlNode[values.Count];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = new ParameterNode<T>(values[i]);
        }

        return nodes;
    }
}
