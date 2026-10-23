namespace AuraORM;

/// <content>
/// Raw queries that read each row as a tuple, column by column (<c>docs/design.ja.md</c> §11.3).
/// </content>
public abstract partial class DatabaseSession<TDialect>
{
    /// <summary>
    /// Runs a raw query and reads the first 2 columns of each row, in order, as a tuple.
    /// </summary>
    /// <typeparam name="T1">The CLR type of column 1.</typeparam>
    /// <typeparam name="T2">The CLR type of column 2.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<(T1, T2)>> QueryAsync<T1, T2>(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default) =>
        Executor.ListAsync(this, Render(sql.ToNode()), static (ref RowReader r) => (r.ReadValue<T1>(), r.ReadValue<T2>()), cancellationToken);

    /// <summary>
    /// Runs a raw query and reads the first 3 columns of each row, in order, as a tuple.
    /// </summary>
    /// <typeparam name="T1">The CLR type of column 1.</typeparam>
    /// <typeparam name="T2">The CLR type of column 2.</typeparam>
    /// <typeparam name="T3">The CLR type of column 3.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<(T1, T2, T3)>> QueryAsync<T1, T2, T3>(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default) =>
        Executor.ListAsync(
            this,
            Render(sql.ToNode()),
            static (ref RowReader r) => (r.ReadValue<T1>(), r.ReadValue<T2>(), r.ReadValue<T3>()),
            cancellationToken
        );

    /// <summary>
    /// Runs a raw query and reads the first 4 columns of each row, in order, as a tuple.
    /// </summary>
    /// <typeparam name="T1">The CLR type of column 1.</typeparam>
    /// <typeparam name="T2">The CLR type of column 2.</typeparam>
    /// <typeparam name="T3">The CLR type of column 3.</typeparam>
    /// <typeparam name="T4">The CLR type of column 4.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<(T1, T2, T3, T4)>> QueryAsync<T1, T2, T3, T4>(SqlInterpolatedStringHandler sql, CancellationToken cancellationToken = default) =>
        Executor.ListAsync(
            this,
            Render(sql.ToNode()),
            static (ref RowReader r) => (r.ReadValue<T1>(), r.ReadValue<T2>(), r.ReadValue<T3>(), r.ReadValue<T4>()),
            cancellationToken
        );

    /// <summary>
    /// Runs a raw query and reads the first 5 columns of each row, in order, as a tuple.
    /// </summary>
    /// <typeparam name="T1">The CLR type of column 1.</typeparam>
    /// <typeparam name="T2">The CLR type of column 2.</typeparam>
    /// <typeparam name="T3">The CLR type of column 3.</typeparam>
    /// <typeparam name="T4">The CLR type of column 4.</typeparam>
    /// <typeparam name="T5">The CLR type of column 5.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<(T1, T2, T3, T4, T5)>> QueryAsync<T1, T2, T3, T4, T5>(
        SqlInterpolatedStringHandler sql,
        CancellationToken cancellationToken = default
    ) =>
        Executor.ListAsync(
            this,
            Render(sql.ToNode()),
            static (ref RowReader r) => (r.ReadValue<T1>(), r.ReadValue<T2>(), r.ReadValue<T3>(), r.ReadValue<T4>(), r.ReadValue<T5>()),
            cancellationToken
        );

    /// <summary>
    /// Runs a raw query and reads the first 6 columns of each row, in order, as a tuple.
    /// </summary>
    /// <typeparam name="T1">The CLR type of column 1.</typeparam>
    /// <typeparam name="T2">The CLR type of column 2.</typeparam>
    /// <typeparam name="T3">The CLR type of column 3.</typeparam>
    /// <typeparam name="T4">The CLR type of column 4.</typeparam>
    /// <typeparam name="T5">The CLR type of column 5.</typeparam>
    /// <typeparam name="T6">The CLR type of column 6.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<(T1, T2, T3, T4, T5, T6)>> QueryAsync<T1, T2, T3, T4, T5, T6>(
        SqlInterpolatedStringHandler sql,
        CancellationToken cancellationToken = default
    ) =>
        Executor.ListAsync(
            this,
            Render(sql.ToNode()),
            static (ref RowReader r) =>
                (r.ReadValue<T1>(), r.ReadValue<T2>(), r.ReadValue<T3>(), r.ReadValue<T4>(), r.ReadValue<T5>(), r.ReadValue<T6>()),
            cancellationToken
        );

    /// <summary>
    /// Runs a raw query and reads the first 7 columns of each row, in order, as a tuple.
    /// </summary>
    /// <typeparam name="T1">The CLR type of column 1.</typeparam>
    /// <typeparam name="T2">The CLR type of column 2.</typeparam>
    /// <typeparam name="T3">The CLR type of column 3.</typeparam>
    /// <typeparam name="T4">The CLR type of column 4.</typeparam>
    /// <typeparam name="T5">The CLR type of column 5.</typeparam>
    /// <typeparam name="T6">The CLR type of column 6.</typeparam>
    /// <typeparam name="T7">The CLR type of column 7.</typeparam>
    /// <param name="sql">The query. Values in holes become parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public Task<List<(T1, T2, T3, T4, T5, T6, T7)>> QueryAsync<T1, T2, T3, T4, T5, T6, T7>(
        SqlInterpolatedStringHandler sql,
        CancellationToken cancellationToken = default
    ) =>
        Executor.ListAsync(
            this,
            Render(sql.ToNode()),
            static (ref RowReader r) =>
                (r.ReadValue<T1>(), r.ReadValue<T2>(), r.ReadValue<T3>(), r.ReadValue<T4>(), r.ReadValue<T5>(), r.ReadValue<T6>(), r.ReadValue<T7>()),
            cancellationToken
        );
}
