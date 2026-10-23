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

    /// <summary>
    /// Gets the alias of the reference (<c>User.Table.As("u")</c>), or <see langword="null"/>.
    /// </summary>
    string? Alias { get; }
}
