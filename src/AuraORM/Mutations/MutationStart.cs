namespace AuraORM;

/// <summary>
/// Starts an <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> for a <see cref="Database{TDialect}"/>, a
/// <see cref="Transaction{TDialect}"/>, or a <see cref="SqlBuilder{TDialect}"/>.
/// </summary>
internal static class MutationStart
{
    public static InsertInto<TDialect, TMode, TColumns, TInsert> Insert<TDialect, TMode, TRow, TColumns, TNullable, TInsert>(
        IStatementRunner? session,
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => new(session, Target(table), table.Info);

    public static UpdateTable<TDialect, TMode, TColumns> Update<TDialect, TMode, TRow, TColumns, TNullable, TInsert>(
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
        TableAlias target = Target(table);
        return new(session, target, TColumns.Create(target));
    }

    public static DeleteFrom<TDialect, TMode, TColumns> Delete<TDialect, TMode, TRow, TColumns, TNullable, TInsert>(
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
        TableAlias target = Target(table);
        return new(session, target, TColumns.Create(target));
    }

    // §6.2: SQL Server gives the target of UPDATE and DELETE an alias only in a FROM clause, so no statement takes one.
    private static TableAlias Target<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(table);
        if (table.Alias is not null)
        {
            throw new ArgumentException(
                "The table that INSERT, UPDATE, or DELETE changes cannot have an alias. Pass the table itself, and give an alias to the table of a subquery instead.",
                nameof(table)
            );
        }

        return ((ITableSource<TColumns, TNullable>)table).CreateOccurrence();
    }
}
