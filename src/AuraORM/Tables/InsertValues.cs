using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// Receives the values of an <see cref="InsertModel"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public ref struct InsertValues
{
    private readonly List<(int Column, ParameterNode Value)> _values;

    internal InsertValues(List<(int Column, ParameterNode Value)> values)
    {
        _values = values;
    }

    /// <summary>
    /// Adds the value of a column.
    /// </summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <param name="column">The position of the column in the table's declaration order.</param>
    /// <param name="value">The value.</param>
    public readonly void Add<T>(int column, T value) => _values.Add((column, new ParameterNode<T>(value)));
}
