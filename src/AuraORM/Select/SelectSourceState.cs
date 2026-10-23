namespace AuraORM;

/// <summary>
/// One source of <c>FROM</c>: the first table, or a joined table with its kind and condition (row Q2).
/// </summary>
/// <param name="Table">The occurrence of the table.</param>
internal sealed record SelectSourceState(TableAlias Table);
