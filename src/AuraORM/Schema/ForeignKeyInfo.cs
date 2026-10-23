namespace AuraORM;

/// <summary>
/// The target of a foreign key.
/// </summary>
/// <param name="Table">The referenced table.</param>
/// <param name="Schema">The schema of the referenced table, or <see langword="null"/>.</param>
/// <param name="Column">The referenced column.</param>
public sealed record ForeignKeyInfo(string Table, string? Schema, string Column);
