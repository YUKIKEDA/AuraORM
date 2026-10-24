namespace AuraORM;

/// <summary>
/// The DDL statements of a transaction, on the dialects whose DDL a rollback undoes (<see cref="ITransactionalDdlDialect"/>:
/// SQLite, PostgreSQL, SQL Server). MySQL and MariaDB commit implicitly before DDL, so a transaction there has none
/// (design §12.5, §13.1).
/// </summary>
public static class TransactionDdl
{
    /// <summary>
    /// Starts a <c>CREATE TABLE</c> in the transaction (design §13.2).
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="ITransactionalDdlDialect"/>.</typeparam>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="transaction">The transaction.</param>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public static CreateTableStatement<TDialect, Executable> CreateTable<TDialect, TRow, TColumns, TNullable, TInsert>(
        this Transaction<TDialect> transaction,
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TDialect : ITransactionalDdlDialect
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return new(transaction, DdlStart.Table(table), ifNotExists: false);
    }

    /// <summary>
    /// Starts the <c>CREATE INDEX</c> statements of the table's indexes in the transaction (design §13.4).
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="ITransactionalDdlDialect"/>.</typeparam>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="transaction">The transaction.</param>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statements.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public static CreateIndexesStatement<TDialect, Executable> CreateIndexes<TDialect, TRow, TColumns, TNullable, TInsert>(
        this Transaction<TDialect> transaction,
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TDialect : ITransactionalDdlDialect
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return new(transaction, DdlStart.Table(table), ifNotExists: false);
    }

    /// <summary>
    /// Starts the <c>DROP INDEX</c> statements of the table's indexes in the transaction (design §13.4).
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="ITransactionalDdlDialect"/>.</typeparam>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="transaction">The transaction.</param>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statements.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public static DropIndexesStatement<TDialect, Executable> DropIndexes<TDialect, TRow, TColumns, TNullable, TInsert>(
        this Transaction<TDialect> transaction,
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TDialect : ITransactionalDdlDialect
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return new(transaction, DdlStart.Table(table), ifExists: false);
    }

    /// <summary>
    /// Starts a <c>DROP TABLE</c> in the transaction (design §13.8).
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="ITransactionalDdlDialect"/>.</typeparam>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="transaction">The transaction.</param>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public static DropTableStatement<TDialect, Executable> DropTable<TDialect, TRow, TColumns, TNullable, TInsert>(
        this Transaction<TDialect> transaction,
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TDialect : ITransactionalDdlDialect
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return new(transaction, DdlStart.Table(table), ifExists: false);
    }
}
