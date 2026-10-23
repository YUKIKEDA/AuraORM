namespace AuraORM;

/// <summary>
/// A column of a table: <see cref="Column{T}"/>, <see cref="ReadOnlyColumn{T}"/>, or <see cref="NullableColumn{T}"/>.
/// The generated column sets list their columns through this interface.
/// </summary>
public interface ITableColumn
{
    /// <summary>
    /// Gets the column node.
    /// </summary>
    internal ColumnNode ColumnNode { get; }
}
