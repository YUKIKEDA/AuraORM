using System.Runtime.CompilerServices;

namespace AuraORM;

/// <summary>
/// Builds a raw SQL fragment from an interpolated string (<c>docs/design.ja.md</c> §11.1). The literal parts become SQL;
/// a hole with an expression, a condition, a column, or a table becomes its SQL; a <see cref="SqlFragment"/> becomes its
/// text; any other value becomes a parameter.
/// </summary>
/// <remarks>
/// The handler has no <c>AppendFormatted</c> overload with a format or an alignment, so <c>{x:N2}</c> and <c>{x,5}</c> do
/// not compile: a value is a parameter, and its text is never formatted into SQL.
/// </remarks>
[InterpolatedStringHandler]
public ref struct SqlInterpolatedStringHandler
{
    private readonly List<SqlNode> _parts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlInterpolatedStringHandler"/> struct. The compiler calls it.
    /// </summary>
    /// <param name="literalLength">The number of literal characters.</param>
    /// <param name="formattedCount">The number of holes.</param>
    public SqlInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _ = literalLength;
        _parts = new List<SqlNode>((formattedCount * 2) + 1);
    }

    /// <summary>
    /// Adds literal SQL text.
    /// </summary>
    /// <param name="value">The text.</param>
    public readonly void AppendLiteral(string value) => _parts.Add(new LiteralNode(value));

    /// <summary>
    /// Adds a hole: the SQL of an expression, condition, column, table, or fragment, or a parameter for any other value.
    /// </summary>
    /// <typeparam name="T">The type of the hole's value.</typeparam>
    /// <param name="value">The value.</param>
    public readonly void AppendFormatted<T>(T value)
    {
        switch (value)
        {
            case ISqlNodeSource source:
                _parts.Add(source.SourceNode);
                break;
            case SqlFragment fragment:
                _parts.Add(fragment.Node);
                break;
            case ITableReference table:
                _parts.Add(new IdentifierNode(table.Info.Schema, table.Info.Name));
                break;
            default:
                _parts.Add(new ParameterNode<T>(value));
                break;
        }
    }

    internal readonly SqlNode ToNode() => new SequenceNode([.. _parts]);
}
