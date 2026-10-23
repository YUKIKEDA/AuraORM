namespace AuraORM;

/// <summary>
/// A SELECT statement with a row limit.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public sealed class SelectLimited<TDialect, TMode, TResult> : SelectQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    internal SelectLimited(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    /// <summary>
    /// Skips rows before the limited ones: <c>LIMIT n OFFSET m</c>, or <c>OFFSET m ROWS FETCH NEXT n ROWS ONLY</c> on SQL
    /// Server (<c>docs/design.ja.md</c> §5.9). The count is a parameter.
    /// </summary>
    /// <param name="count">The number of rows to skip.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public SelectOffset<TDialect, TMode, TResult> Offset(int count) =>
        new(Session, State with { Offset = SelectState.RowCount(count, nameof(count)) }, Factory);
}
