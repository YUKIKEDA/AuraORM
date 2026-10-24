using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// Builds statements without a connection or a provider, for their SQL only (<c>docs/design.ja.md</c> §12.3, R9). It has the
/// same statement entry points as <see cref="Database{TDialect}"/>; the statements are <see cref="SqlOnly"/>, so they have
/// <c>ToSql()</c> and no run methods.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
public sealed class SqlBuilder<TDialect>
    where TDialect : IDialect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilder{TDialect}"/> class.
    /// </summary>
    public SqlBuilder()
    {
        _ = DialectRenderer.For<TDialect>();
    }

    /// <summary>
    /// Starts a SELECT statement: <c>SELECT … FROM t</c>. The lambdas that follow get the column set of the table or the
    /// named query (<c>docs/design.ja.md</c> §5.1, §5.11).
    /// </summary>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <param name="table">A table, such as <c>User.Table</c>, or a named query (<c>query.As("t")</c>).</param>
    /// <returns>The statement.</returns>
    public SelectFrom<TDialect, SqlOnly, TColumns, TNullable> From<TColumns, TNullable>(ITableSource<TColumns, TNullable> table)
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable> =>
        SelectStart.From<TDialect, SqlOnly, TColumns, TNullable>(session: null, table, ImmutableArray<NamedQueryDefinition>.Empty);

    /// <summary>
    /// Starts a SELECT statement with a <c>WITH</c>: the named queries are written as CTEs, in order, and a later one can use
    /// an earlier one (<c>docs/design.ja.md</c> §5.11).
    /// </summary>
    /// <param name="queries">The named queries, each itself and not an aliased reference.</param>
    /// <returns>The start of the statement.</returns>
    /// <exception cref="ArgumentException">A query is an aliased reference, or two have the same name.</exception>
    public SelectWith<TDialect, SqlOnly> With(params NamedQuery[] queries) => new(session: null, SelectStart.Ctes(queries));

    /// <summary>
    /// Starts an <c>INSERT</c> into a table: <c>Insert(User.Table).Values(new User.Insert { … })</c>
    /// (<c>docs/design.ja.md</c> §6).
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <typeparam name="TInsert">The <c>INSERT</c> model.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The start of the statement.</returns>
    /// <exception cref="ArgumentException">The table has an alias (<c>docs/design.ja.md</c> §6.2).</exception>
    public InsertInto<TDialect, SqlOnly, TColumns, TInsert> Insert<TRow, TColumns, TNullable, TInsert>(
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => MutationStart.Insert<TDialect, SqlOnly, TRow, TColumns, TNullable, TInsert>(session: null, table);

    /// <summary>
    /// Starts an <c>UPDATE</c> of a table: <c>Update(User.Table).Set(u =&gt; [Set(u.Name, "Bob")]).Where(…)</c>
    /// (<c>docs/design.ja.md</c> §7).
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <typeparam name="TInsert">The <c>INSERT</c> model.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The start of the statement.</returns>
    /// <exception cref="ArgumentException">The table has an alias (<c>docs/design.ja.md</c> §6.2).</exception>
    public UpdateTable<TDialect, SqlOnly, TColumns> Update<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => MutationStart.Update<TDialect, SqlOnly, TRow, TColumns, TNullable, TInsert>(session: null, table);

    /// <summary>
    /// Starts a <c>DELETE</c> from a table: <c>Delete(User.Table).Where(…)</c> (<c>docs/design.ja.md</c> §8).
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <typeparam name="TInsert">The <c>INSERT</c> model.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The start of the statement.</returns>
    /// <exception cref="ArgumentException">The table has an alias (<c>docs/design.ja.md</c> §6.2).</exception>
    public DeleteFrom<TDialect, SqlOnly, TColumns> Delete<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => MutationStart.Delete<TDialect, SqlOnly, TRow, TColumns, TNullable, TInsert>(session: null, table);

    /// <summary>
    /// Starts a <c>CREATE TABLE</c> (design §13.2).
    /// </summary>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public CreateTableStatement<TDialect, SqlOnly> CreateTable<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => new(session: null, DdlStart.Table(table), ifNotExists: false);

    /// <summary>
    /// Starts the <c>CREATE INDEX</c> statements of the table's <see cref="IndexAttribute"/> indexes (design §13.4).
    /// </summary>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public CreateIndexesStatement<TDialect, SqlOnly> CreateIndexes<TRow, TColumns, TNullable, TInsert>(
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => new(session: null, DdlStart.Table(table), ifNotExists: false);

    /// <summary>
    /// Starts the <c>DROP INDEX</c> statements of the table's <see cref="IndexAttribute"/> indexes (design §13.4).
    /// </summary>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public DropIndexesStatement<TDialect, SqlOnly> DropIndexes<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => new(session: null, DdlStart.Table(table), ifExists: false);

    /// <summary>
    /// Starts a <c>DROP TABLE</c> (design §13.8).
    /// </summary>
    /// <typeparam name="TRow">The row type of the table.</typeparam>
    /// <typeparam name="TColumns">The columns of the table.</typeparam>
    /// <typeparam name="TNullable">The nullable columns of the table.</typeparam>
    /// <typeparam name="TInsert">The insert model of the table.</typeparam>
    /// <param name="table">The table, without an alias.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException"><paramref name="table"/> has an alias.</exception>
    public DropTableStatement<TDialect, SqlOnly> DropTable<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => new(session: null, DdlStart.Table(table), ifExists: false);
}
