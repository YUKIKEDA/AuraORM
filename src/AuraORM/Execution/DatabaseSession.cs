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
    /// Starts a SELECT statement: <c>FROM table</c> (<c>docs/design.ja.md</c> §5). The lambdas of the statement get the
    /// table's column set.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <typeparam name="TInsert">The <c>INSERT</c> model.</typeparam>
    /// <param name="table">The table reference, such as <c>User.Table</c>.</param>
    /// <returns>The statement.</returns>
    public SelectFrom<TDialect, Executable, TColumns, TNullable> From<TRow, TColumns, TNullable, TInsert>(
        Table<TRow, TColumns, TNullable, TInsert> table
    )
        where TRow : class
        where TColumns : TableColumns<TRow>, ITableColumns<TColumns>
        where TNullable : NullableTableColumns<TRow>, ITableColumns<TNullable>
        where TInsert : InsertModel => SelectStart.From<TDialect, Executable, TRow, TColumns, TNullable, TInsert>(this, table);

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
