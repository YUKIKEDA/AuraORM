namespace AuraORM;

/// <summary>
/// An item of the result of a set operation, as the <c>OrderBy</c> lambda gets it (<c>docs/design.ja.md</c> §5.10). It is not
/// an expression: it can only be passed to <see cref="Sql.Asc{T, TLevel}(SetItem{T, TLevel})"/> and
/// <see cref="Sql.Desc{T, TLevel}(SetItem{T, TLevel})"/>, which name it by its position, because each operand selects it from
/// its own tables.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the first statement's projection.</typeparam>
public sealed class SetItem<T, TLevel>
    where TLevel : EvaluationLevel
{
    internal SetItem(int position)
    {
        Position = position;
    }

    /// <summary>
    /// Gets the position of the item in the select list, from 1.
    /// </summary>
    internal int Position { get; }
}
