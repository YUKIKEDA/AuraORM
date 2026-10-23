namespace AuraORM;

/// <content>
/// The keys of <c>ORDER BY</c> (<c>docs/design.ja.md</c> §5.8). The direction is always written.
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Orders by an expression, smallest first: <c>a ASC</c>.
    /// </summary>
    /// <typeparam name="T">The type of the expression.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the expression.</typeparam>
    /// <param name="key">The expression.</param>
    /// <returns>The key.</returns>
    public static Ordering<TLevel> Asc<T, TLevel>(IExpr<T, TLevel> key)
        where TLevel : EvaluationLevel => new(key.Node, descending: false);

    /// <summary>
    /// Orders by an expression, largest first: <c>a DESC</c>.
    /// </summary>
    /// <typeparam name="T">The type of the expression.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the expression.</typeparam>
    /// <param name="key">The expression.</param>
    /// <returns>The key.</returns>
    public static Ordering<TLevel> Desc<T, TLevel>(IExpr<T, TLevel> key)
        where TLevel : EvaluationLevel => new(key.Node, descending: true);
}
