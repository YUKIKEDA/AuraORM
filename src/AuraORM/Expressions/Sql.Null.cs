namespace AuraORM;

/// <content>
/// The functions for <c>NULL</c> (<c>docs/design.ja.md</c> §4.2).
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Tests whether a value is null: <c>a IS NULL</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> IsNull<T, TLevel>(IExpr<T, TLevel> value)
        where TLevel : EvaluationLevel => new(new IsNullNode(value.Node, negated: false));

    /// <summary>
    /// Tests whether a value is not null: <c>a IS NOT NULL</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> IsNotNull<T, TLevel>(IExpr<T, TLevel> value)
        where TLevel : EvaluationLevel => new(new IsNullNode(value.Node, negated: true));

    /// <summary>
    /// Compares two values for equality, treating two nulls as equal: <c>a IS NOT DISTINCT FROM b</c> on PostgreSQL,
    /// <c>a IS b</c> on SQLite, <c>a &lt;=&gt; b</c> on MySQL and MariaDB, and an expansion with <c>OR</c> on SQL Server.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> IsNotDistinctFrom<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, right.Node, distinct: false));

    /// <inheritdoc cref="IsNotDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> IsNotDistinctFrom<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, right.Node, distinct: false));

    /// <inheritdoc cref="IsNotDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> IsNotDistinctFrom<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, right.Node, distinct: false));

    /// <inheritdoc cref="IsNotDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> IsNotDistinctFrom<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, Param(right), distinct: false));

    /// <summary>
    /// Compares two values for inequality, treating two nulls as equal: <c>a IS DISTINCT FROM b</c> on PostgreSQL,
    /// <c>a IS NOT b</c> on SQLite, <c>NOT (a &lt;=&gt; b)</c> on MySQL and MariaDB, and an expansion with <c>OR</c> on SQL Server.
    /// </summary>
    /// <typeparam name="T">The type of both operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> IsDistinctFrom<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, right.Node, distinct: true));

    /// <inheritdoc cref="IsDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> IsDistinctFrom<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, right.Node, distinct: true));

    /// <inheritdoc cref="IsDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> IsDistinctFrom<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, right.Node, distinct: true));

    /// <inheritdoc cref="IsDistinctFrom{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static Condition<TLevel> IsDistinctFrom<T, TLevel>(IExpr<T, TLevel> left, T right)
        where TLevel : EvaluationLevel => new(new DistinctFromNode(left.Node, Param(right), distinct: true));

    /// <summary>
    /// Returns the first operand if it is not null, otherwise the second: <c>COALESCE(a, b)</c>. Nest calls for more operands.
    /// </summary>
    /// <typeparam name="T">The non-nullable value type of the result.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="value">The value that may be null.</param>
    /// <param name="fallback">The value used when <paramref name="value"/> is null.</param>
    /// <returns>The expression. It is not nullable, because <paramref name="fallback"/> is not.</returns>
    public static IExpr<T, TLevel> Coalesce<T, TLevel>(IExpr<T?, TLevel> value, IExpr<T, TLevel> fallback)
        where T : struct
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Coalesce, value.Node, fallback.Node);

    /// <inheritdoc cref="Coalesce{T, TLevel}(IExpr{T?, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Coalesce<T, TLevel>(IExpr<T?, TLevel> value, T fallback)
        where T : struct
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Coalesce, value.Node, Param(fallback));

    /// <summary>
    /// Returns the first operand if it is not null, otherwise the second: <c>COALESCE(a, b)</c>. Nest calls for more operands.
    /// </summary>
    /// <typeparam name="T">The type of the result. For strings, it is nullable when <paramref name="fallback"/> is.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="value">The value that may be null.</param>
    /// <param name="fallback">The value used when <paramref name="value"/> is null.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Coalesce<T, TLevel>(IExpr<T, TLevel> value, IExpr<T, TLevel> fallback)
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Coalesce, value.Node, fallback.Node);

    /// <inheritdoc cref="Coalesce{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Coalesce<T, TLevel>(IExpr<T, TLevel> value, T fallback)
        where TLevel : EvaluationLevel => Function<T, TLevel>(SqlFunction.Coalesce, value.Node, Param(fallback));

    /// <summary>
    /// Returns null if the two operands are equal, otherwise the first: <c>NULLIF(a, b)</c>.
    /// </summary>
    /// <typeparam name="T">The value type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="other">The value that turns the result into null.</param>
    /// <returns>The nullable expression.</returns>
    public static IExpr<T?, TLevel> NullIf<T, TLevel>(IExpr<T, TLevel> value, T other)
        where T : struct
        where TLevel : EvaluationLevel => Function<T?, TLevel>(SqlFunction.NullIf, value.Node, Param(other));

    /// <inheritdoc cref="NullIf{T, TLevel}(IExpr{T, TLevel}, T)"/>
    public static IExpr<T?, TLevel> NullIf<T, TLevel>(IExpr<T?, TLevel> value, T other)
        where T : struct
        where TLevel : EvaluationLevel => Function<T?, TLevel>(SqlFunction.NullIf, value.Node, Param(other));

    /// <inheritdoc cref="NullIf{T, TLevel}(IExpr{T, TLevel}, T)"/>
    public static IExpr<string?, TLevel> NullIf<TLevel>(IExpr<string?, TLevel> value, string other)
        where TLevel : EvaluationLevel => Function<string?, TLevel>(SqlFunction.NullIf, value.Node, Param(other));

    private static SqlExpr<T, TLevel> Function<T, TLevel>(SqlFunction function, params SqlNode[] arguments)
        where TLevel : EvaluationLevel => new(new FunctionNode(function, arguments));
}
