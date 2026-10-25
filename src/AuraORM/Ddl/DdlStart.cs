namespace AuraORM;

/// <summary>
/// Starts the DDL statements of <see cref="Database{TDialect}"/>, <see cref="Transaction{TDialect}"/>, and
/// <see cref="SqlBuilder{TDialect}"/>.
/// </summary>
internal static class DdlStart
{
    public static TableInfo Table<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(table);
        if (table.Alias is not null)
        {
            throw new ArgumentException("DDL takes the table itself, not an alias of it; pass the table without As(...).", nameof(table));
        }

        return table.Info;
    }
}
