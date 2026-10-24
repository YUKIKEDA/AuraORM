namespace AuraORM;

/// <summary>
/// What a statement's <c>FROM</c> or a join can name: a table (<see cref="Table{TRow, TColumns, TNullable, TInsert}"/>) or a
/// named query (<see cref="NamedQuery{TColumns, TNullable}"/>, <c>docs/design.ja.md</c> §5.11). The lambdas that follow get
/// its column set.
/// </summary>
/// <typeparam name="TColumns">The column set.</typeparam>
/// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
public interface ITableSource<TColumns, TNullable>
    where TColumns : ITableColumns<TColumns>
    where TNullable : ITableColumns<TNullable>
{
    /// <summary>
    /// Creates the occurrence of the source in a statement.
    /// </summary>
    /// <returns>The occurrence.</returns>
    internal TableAlias CreateOccurrence();
}
