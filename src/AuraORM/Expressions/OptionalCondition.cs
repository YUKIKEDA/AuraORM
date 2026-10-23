namespace AuraORM;

/// <summary>
/// A condition that may be empty: the result of <see cref="Sql.And{TLevel}(IEnumerable{Condition{TLevel}})"/> or
/// <see cref="Sql.Or{TLevel}(IEnumerable{Condition{TLevel}})"/> over a list whose items are known only at run time
/// (<c>docs/design.ja.md</c> §4.2). A SELECT <c>Where</c> with an empty condition writes no <c>WHERE</c>.
/// </summary>
/// <typeparam name="TLevel">The evaluation level: <see cref="PerRow"/> or <see cref="PerGroup"/>.</typeparam>
/// <remarks>
/// The <c>Where</c> of <c>UPDATE</c> and <c>DELETE</c> does not take it: an empty list would make the statement change every
/// row. Write <c>AllRows()</c> for that.
/// </remarks>
public sealed class OptionalCondition<TLevel>
    where TLevel : EvaluationLevel
{
    internal OptionalCondition(SqlNode? node)
    {
        Node = node;
    }

    /// <summary>
    /// Gets a value indicating whether the list had no condition.
    /// </summary>
    public bool IsEmpty => Node is null;

    internal SqlNode? Node { get; }
}
