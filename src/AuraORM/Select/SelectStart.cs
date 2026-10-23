namespace AuraORM;

/// <summary>
/// Starts a SELECT statement for a <see cref="Database{TDialect}"/>, a <see cref="Transaction{TDialect}"/>, or a
/// <see cref="SqlBuilder{TDialect}"/>.
/// </summary>
internal static class SelectStart
{
    public static SelectFrom<TDialect, TMode, TColumns, TNullable> From<TDialect, TMode, TRow, TColumns, TNullable, TInsert>(
        IStatementRunner? session,
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(table);
        var alias = new TableAlias(table.Info.Schema, table.Info.Name, alias: null);
        return new SelectFrom<TDialect, TMode, TColumns, TNullable>(session, SelectState.From(alias), TColumns.Create(alias));
    }
}
