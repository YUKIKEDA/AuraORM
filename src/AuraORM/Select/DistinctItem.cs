namespace AuraORM;

/// <summary>
/// An item of the select list of a <c>SELECT DISTINCT</c>, as the <c>OrderBy</c> lambda gets it (<c>docs/design.ja.md</c>
/// §5.7). It is not an expression: it can only be passed to <see cref="Sql.Asc{T, TLevel}(DistinctItem{T, TLevel})"/> and
/// <see cref="Sql.Desc{T, TLevel}(DistinctItem{T, TLevel})"/>, because PostgreSQL and SQL Server reject an <c>ORDER BY</c>
/// expression of a <c>SELECT DISTINCT</c> that is not in the select list.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the item.</typeparam>
public sealed class DistinctItem<T, TLevel>
    where TLevel : EvaluationLevel
{
    internal DistinctItem(int position)
    {
        Position = position;
    }

    /// <summary>
    /// Gets the position of the item in the select list, from 1. <c>ORDER BY</c> names the item by this position.
    /// </summary>
    internal int Position { get; }
}
