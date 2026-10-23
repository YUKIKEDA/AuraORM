namespace AuraORM;

/// <summary>
/// The select list of a query and the code that reads its result as <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type that one row of the result is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of every item: <see cref="PerRow"/> or <see cref="PerGroup"/>.</typeparam>
/// <remarks>
/// Expressions, whole rows, tuples, and result types are projections. Only AuraORM and the code its source generator emits
/// implement this interface.
/// </remarks>
public interface IProjection<out T, TLevel>
    where TLevel : EvaluationLevel
{
    /// <summary>
    /// Adds the select items of this projection, in order.
    /// </summary>
    /// <param name="items">The list that receives the items.</param>
    internal void CollectSelectItems(List<SqlNode> items);
}
