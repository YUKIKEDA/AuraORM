namespace AuraORM;

/// <content>
/// The correlation of a group subquery with the grouped statement (<c>docs/design.ja.md</c> §5.11).
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Uses a <c>GROUP BY</c> key of the grouped statement inside the subquery of
    /// <see cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/> or
    /// <see cref="GroupExists{TResult, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, TResult, TSubLevel})"/>,
    /// where the subquery's lambdas take row values: <c>Where(q =&gt; Eq(q.UserId, Outer(k)))</c>. It writes the key's column.
    /// </summary>
    /// <typeparam name="T">The type of the key.</typeparam>
    /// <param name="key">The key, as the grouped statement's lambdas get it (<c>k</c> or <c>k.Item1</c> …).</param>
    /// <returns>The key as a row value of the subquery.</returns>
    /// <exception cref="ArgumentException">The argument is not a key, or the key is not a column: an expression key would
    /// need <c>MIN</c> on some dialects and fail with it on SQLite.</exception>
    /// <remarks>Used outside those subqueries, the statement throws <see cref="InvalidOperationException"/> when it is
    /// built.</remarks>
    public static IExpr<T, PerRow> Outer<T>(IExpr<T, PerGroup> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Node is not GroupKeyNode { Key: ColumnNode } groupKey)
        {
            throw new ArgumentException(
                "Outer takes a GROUP BY key that is a column (k or k.Item1 …), not an aggregate or an expression.",
                nameof(key)
            );
        }

        return new SqlExpr<T, PerRow>(new OuterKeyNode(groupKey));
    }
}
