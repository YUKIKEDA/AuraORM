namespace AuraORM;

/// <summary>
/// One source of <c>FROM</c>: the first table, or a joined table with its kind and condition (<c>docs/design.ja.md</c>
/// §5.3).
/// </summary>
/// <param name="Table">The occurrence of the table.</param>
/// <param name="Kind">How it joins the sources before it.</param>
/// <param name="On">The condition of <c>ON</c>, or <see langword="null"/> for the first table and <c>CROSS JOIN</c>.</param>
internal sealed record SelectSourceState(TableAlias Table, JoinKind Kind, SqlNode? On);
