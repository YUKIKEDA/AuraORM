namespace AuraORM;

/// <summary>
/// A table reference without its type arguments, so that a raw fragment can write it.
/// </summary>
internal interface ITableReference
{
    /// <summary>
    /// Gets what the schema declares about the table.
    /// </summary>
    TableInfo Info { get; }
}
