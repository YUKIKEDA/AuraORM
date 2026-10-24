using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// The statements that a <see cref="Database{TDialect}"/> and a <see cref="Transaction{TDialect}"/> both run.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <remarks>
/// A statement is rendered when its method is called, before the returned task starts. An error found then, such as more
/// parameters than the dialect accepts, is thrown by the call itself and not through the task (<c>docs/design.ja.md</c>
/// §14.3).
/// </remarks>
public abstract partial class DatabaseSession<TDialect> : IStatementRunner
    where TDialect : IDialect
{
    private protected DatabaseSession(DatabaseOptions options)
    {
        Options = options;
    }

    /// <inheritdoc />
    DialectRenderer IStatementRunner.Renderer => DialectRenderer.For<TDialect>();

    /// <summary>
    /// Gets the settings of the database.
    /// </summary>
    public DatabaseOptions Options { get; }

    /// <summary>
    /// Starts a SELECT statement: <c>SELECT … FROM t</c>. The lambdas that follow get the column set of the table or the
    /// named query (<c>docs/design.ja.md</c> §5.1, §5.11).
    /// </summary>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <param name="table">A table, such as <c>User.Table</c>, or a named query (<c>query.As("t")</c>).</param>
    /// <returns>The statement.</returns>
    public SelectFrom<TDialect, Executable, TColumns, TNullable> From<TColumns, TNullable>(ITableSource<TColumns, TNullable> table)
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable> =>
        SelectStart.From<TDialect, Executable, TColumns, TNullable>(this, table, ImmutableArray<NamedQueryDefinition>.Empty);

    /// <summary>
    /// Starts a SELECT statement with a <c>WITH</c>: the named queries are written as CTEs, in order, and a later one can use
    /// an earlier one (<c>docs/design.ja.md</c> §5.11).
    /// </summary>
    /// <param name="queries">The named queries, each itself and not an aliased reference.</param>
    /// <returns>The start of the statement.</returns>
    /// <exception cref="ArgumentException">A query is an aliased reference, or two have the same name.</exception>
    public SelectWith<TDialect, Executable> With(params NamedQuery[] queries) => new(this, SelectStart.Ctes(queries));

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
    public InsertInto<TDialect, Executable, TColumns, TInsert> Insert<TRow, TColumns, TNullable, TInsert>(
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => MutationStart.Insert<TDialect, Executable, TRow, TColumns, TNullable, TInsert>(this, table);

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
    public UpdateTable<TDialect, Executable, TColumns> Update<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => MutationStart.Update<TDialect, Executable, TRow, TColumns, TNullable, TInsert>(this, table);

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
    public DeleteFrom<TDialect, Executable, TColumns> Delete<TRow, TColumns, TNullable, TInsert>(Table<TRow, TColumns, TNullable, TInsert> table)
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => MutationStart.Delete<TDialect, Executable, TRow, TColumns, TNullable, TInsert>(this, table);

    /// <summary>
    /// Runs a raw query and reads each row into a <c>[Table]</c> or <c>[QueryResult]</c> type by column name
    /// (<c>docs/design.ja.md</c> §11.3).
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    /// <exception cref="InvalidOperationException">The result has no column for a member of <typeparamref name="T"/>, or has
    /// two columns with the name of one member.</exception>
    public Task<List<T>> QueryAsync<T>(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default)
        where T : IQueryResult<T> => Executor.ListNamedAsync<T>(this, Render(sql.ToNode()), cancellationToken);

    /// <summary>
    /// Runs a raw query and returns the first column of the first row.
    /// </summary>
    /// <typeparam name="T">The CLR type of the value.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The query returned no row.</exception>
    public Task<T> QueryScalarAsync<T>(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default) =>
        Executor.ScalarAsync<T>(this, Render(sql.ToNode()), orDefault: false, cancellationToken);

    /// <summary>
    /// Runs a raw query and returns the first column of the first row, or <see langword="default"/> when there is no row.
    /// For a value type, read it as nullable (<c>int?</c>) to tell no row from a row whose value is the default.
    /// </summary>
    /// <typeparam name="T">The CLR type of the value.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value, or <see langword="default"/>.</returns>
    public Task<T?> QueryScalarOrDefaultAsync<T>(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default) =>
        Executor.ScalarAsync<T?>(this, Render(sql.ToNode()), orDefault: true, cancellationToken);

    /// <summary>
    /// Runs a raw command.
    /// </summary>
    /// <param name="sql">The command. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected, as the provider reports it.</returns>
    public Task<int> ExecuteAsync(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default) =>
        Executor.ExecuteAsync(this, Render(sql.ToNode()), cancellationToken);

    /// <inheritdoc />
    ValueTask<ConnectionLease> IStatementRunner.LeaseAsync(CancellationToken cancellationToken) => LeaseAsync(cancellationToken);

    private protected abstract ValueTask<ConnectionLease> LeaseAsync(CancellationToken cancellationToken);

    private protected static SqlStatement Render(SqlNode node) => SqlRenderContext.Render(DialectRenderer.For<TDialect>(), node);
}
