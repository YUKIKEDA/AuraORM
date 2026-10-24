namespace AuraORM;

/// <summary>
/// Turns a <c>GROUP BY</c> key into the <see cref="PerGroup"/> expression that the later lambdas get
/// (<c>docs/design.ja.md</c> §4.5, §5.5).
/// </summary>
internal static class GroupKey
{
    /// <summary>
    /// Returns the key as a <see cref="PerGroup"/> expression with the same node, so that the key written again in the select
    /// list, <c>HAVING</c>, or <c>ORDER BY</c> renders as the same SQL with the same parameters (§5.12).
    /// </summary>
    /// <typeparam name="T">The type of the key.</typeparam>
    /// <param name="key">The key.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, PerGroup> Of<T>(IExpr<T, PerRow> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new SqlExpr<T, PerGroup>(new GroupKeyNode(key.Node));
    }
}
