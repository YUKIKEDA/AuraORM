namespace AuraORM;

/// <content>
/// Casts, <c>CASE</c>, constants, and the current time (<c>docs/design.ja.md</c> §4.2, §4.3).
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Converts a value to another type: <c>CAST(a AS type)</c>, with the type name of the dialect.
    /// </summary>
    /// <typeparam name="TSource">The type of the value.</typeparam>
    /// <typeparam name="TTarget">The value type of the result.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="type">The target type, a property of <see cref="SqlType"/>.</param>
    /// <returns>The nullable expression. What a failed conversion gives (null or an error) depends on the dialect.</returns>
    public static IExpr<TTarget?, TLevel> Cast<TSource, TTarget, TLevel>(IExpr<TSource, TLevel> value, CastType<TTarget> type)
        where TTarget : struct
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(type);
        return new SqlExpr<TTarget?, TLevel>(new CastNode(value.Node, type.Target));
    }

    /// <inheritdoc cref="Cast{TSource, TTarget, TLevel}(IExpr{TSource, TLevel}, CastType{TTarget})"/>
    public static IExpr<string?, TLevel> Cast<TSource, TLevel>(IExpr<TSource, TLevel> value, CastType<string> type)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(type);
        return new SqlExpr<string?, TLevel>(new CastNode(value.Node, type.Target));
    }

    /// <summary>
    /// Starts a <c>CASE</c> expression with its first branch: <c>CASE WHEN condition THEN result …</c>.
    /// Finish it with <see cref="CaseBuilder{T, TLevel}.Else(T)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <typeparam name="TLevel">The evaluation level.</typeparam>
    /// <param name="condition">The condition of the first branch.</param>
    /// <param name="result">The result of the first branch.</param>
    /// <returns>The builder of the <c>CASE</c> expression.</returns>
    public static CaseBuilder<T, TLevel> Case<T, TLevel>(Condition<TLevel> condition, IExpr<T, TLevel> result)
        where TLevel : EvaluationLevel => new CaseBuilder<T, TLevel>().When(condition, result);

    /// <inheritdoc cref="Case{T, TLevel}(Condition{TLevel}, IExpr{T, TLevel})"/>
    public static CaseBuilder<T, TLevel> Case<T, TLevel>(Condition<TLevel> condition, T result)
        where TLevel : EvaluationLevel => new CaseBuilder<T, TLevel>().When(condition, result);

    /// <summary>
    /// Uses a value as an expression, bound as a parameter: <c>@p0</c>. Functions take values directly; this is for a value
    /// in a projection.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, PerRow> Value<T>(T value) => new SqlExpr<T, PerRow>(Param(value));

    /// <summary>
    /// Returns the database's current date and time: <c>CURRENT_TIMESTAMP</c>.
    /// </summary>
    /// <returns>The expression.</returns>
    public static IExpr<DateTime, PerRow> CurrentTimestamp() => Function<DateTime, PerRow>(SqlFunction.CurrentTimestamp);
}
