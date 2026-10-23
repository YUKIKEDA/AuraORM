namespace AuraORM;

/// <summary>
/// An index of a table, from <see cref="IndexAttribute"/>.
/// </summary>
/// <param name="Name">The index name.</param>
/// <param name="Columns">The column names, in order.</param>
/// <param name="IsUnique">A value indicating whether the index is unique.</param>
public sealed record IndexInfo(string Name, IReadOnlyList<string> Columns, bool IsUnique);
