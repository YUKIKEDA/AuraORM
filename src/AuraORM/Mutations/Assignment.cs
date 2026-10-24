namespace AuraORM;

/// <summary>
/// One assignment of an <c>UPDATE</c>: <c>"column" = value</c> (<c>docs/design.ja.md</c> §7). Create it with
/// <see cref="Sql.Set{T}(Column{T}, T)"/>.
/// </summary>
public sealed class Assignment
{
    internal Assignment(ColumnNode column, SqlNode value)
    {
        Column = column;
        Value = value;
    }

    internal ColumnNode Column { get; }

    internal SqlNode Value { get; }
}
