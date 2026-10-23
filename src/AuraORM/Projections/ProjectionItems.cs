using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// Receives the select items of a <see cref="ResultProjection{TResult, TLevel}"/>.
/// </summary>
/// <typeparam name="TLevel">The evaluation level of every item.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct ProjectionItems<TLevel>
    where TLevel : EvaluationLevel
{
    private readonly List<SqlNode> _items;

    internal ProjectionItems(List<SqlNode> items)
    {
        _items = items;
    }

    /// <summary>
    /// Adds the select items of a member's projection.
    /// </summary>
    /// <typeparam name="T">The type of the member.</typeparam>
    /// <param name="projection">The projection.</param>
    public void Add<T>(IProjection<T, TLevel> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        projection.CollectSelectItems(_items);
    }
}
