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
        where TLevel : EvaluationLevel => new(DecimalOrderingNode.For<T>(key.Node, "ORDER BY"), descending: false);

    /// <summary>
    /// Orders by an expression, largest first: <c>a DESC</c>.
    /// </summary>
    /// <typeparam name="T">The type of the expression.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the expression.</typeparam>
    /// <param name="key">The expression.</param>
    /// <returns>The key.</returns>
    public static Ordering<TLevel> Desc<T, TLevel>(IExpr<T, TLevel> key)
        where TLevel : EvaluationLevel => new(DecimalOrderingNode.For<T>(key.Node, "ORDER BY"), descending: true);

    /// <summary>
    /// Orders a <c>SELECT DISTINCT</c> by one of its items, smallest first: <c>n ASC</c>, where <c>n</c> is the item's
    /// position in the select list (<c>docs/design.ja.md</c> §5.7).
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the item.</typeparam>
    /// <param name="key">The item.</param>
    /// <returns>The key.</returns>
    public static Ordering<TLevel> Asc<T, TLevel>(DistinctItem<T, TLevel> key)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(key);
        return new(DecimalOrderingNode.For<T>(new PositionNode(key.Position), "ORDER BY"), descending: false);
    }

    /// <summary>
    /// Orders a <c>SELECT DISTINCT</c> by one of its items, largest first: <c>n DESC</c>, where <c>n</c> is the item's
    /// position in the select list (<c>docs/design.ja.md</c> §5.7).
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the item.</typeparam>
    /// <param name="key">The item.</param>
    /// <returns>The key.</returns>
    public static Ordering<TLevel> Desc<T, TLevel>(DistinctItem<T, TLevel> key)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(key);
        return new(DecimalOrderingNode.For<T>(new PositionNode(key.Position), "ORDER BY"), descending: true);
    }
}
