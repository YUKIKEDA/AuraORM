namespace AuraORM;

/// <summary>
/// A SELECT statement after <c>ORDER BY</c>.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public sealed class SelectOrdered<TDialect, TResult> : SelectQuery<TDialect, TResult>
    where TDialect : IDialect
{
    internal SelectOrdered(IStatementRunner session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    /// <summary>
    /// Limits the number of rows: <c>LIMIT n</c>, or <c>OFFSET 0 ROWS FETCH NEXT n ROWS ONLY</c> on SQL Server
    /// (<c>docs/design.ja.md</c> §5.9). The count is a parameter.
    /// </summary>
    /// <param name="count">The largest number of rows.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public SelectLimited<TDialect, TResult> Limit(int count) =>
        new(Session, State with { Limit = SelectState.RowCount(count, nameof(count)) }, Factory);

    internal SelectOffset<TDialect, TResult> OffsetCore(int count) =>
        new(Session, State with { Offset = SelectState.RowCount(count, nameof(count)) }, Factory);
}
