namespace AuraORM;

/// <summary>
/// One key of <c>ORDER BY</c>.
/// </summary>
/// <param name="Node">The expression.</param>
/// <param name="Descending">A value indicating whether the order is <c>DESC</c>.</param>
internal sealed record OrderKey(SqlNode Node, bool Descending);
