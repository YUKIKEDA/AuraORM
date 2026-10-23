namespace AuraORM;

/// <summary>
/// One key of <c>ORDER BY</c>: an expression and its direction. Created by <see cref="Sql.Asc{T, TLevel}(IExpr{T, TLevel})"/>
/// and <see cref="Sql.Desc{T, TLevel}(IExpr{T, TLevel})"/>.
/// </summary>
/// <typeparam name="TLevel">The evaluation level of the expression.</typeparam>
public sealed class Ordering<TLevel>
    where TLevel : EvaluationLevel
{
    internal Ordering(SqlNode node, bool descending)
    {
        Key = new OrderKey(node, descending);
    }

    internal OrderKey Key { get; }
}
