namespace AuraORM;

/// <summary>
/// A SELECT statement after <c>ORDER BY</c>.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public sealed class SelectOrdered<TDialect, TMode, TResult> : SelectQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    internal SelectOrdered(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    /// <summary>
    /// Limits the number of rows: <c>LIMIT n</c>, or <c>OFFSET 0 ROWS FETCH NEXT n ROWS ONLY</c> on SQL Server
    /// (<c>docs/design.ja.md</c> §5.9). The count is a parameter.
    /// </summary>
    /// <param name="count">The largest number of rows.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative, or 0 on SQL Server, whose
    /// <c>FETCH</c> needs at least one row.</exception>
    public SelectLimited<TDialect, TMode, TResult> Limit(int count) =>
        new(Session, State with { Limit = SelectState.RowLimit<TDialect>(count, nameof(count)) }, Factory);

    internal SelectOffset<TDialect, TMode, TResult> OffsetCore(int count) =>
        new(Session, State with { Offset = SelectState.RowCount(count, nameof(count)) }, Factory);

    /// <summary>Not accepted: a set operation's operand cannot be ordered or limited.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.OrderedOperand, error: true)]
    public SelectUnion<TDialect, TMode, TResult, PerRow> Union<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.OrderedOperand);

    /// <summary>Not accepted: a set operation's operand cannot be ordered or limited.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.OrderedOperand, error: true)]
    public SelectUnionAll<TDialect, TMode, TResult, PerRow> UnionAll<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.OrderedOperand);

    /// <summary>Not accepted: a set operation's operand cannot be ordered or limited.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.OrderedOperand, error: true)]
    public SelectIntersect<TDialect, TMode, TResult, PerRow> Intersect<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.OrderedOperand);

    /// <summary>Not accepted: a set operation's operand cannot be ordered or limited.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.OrderedOperand, error: true)]
    public SelectExcept<TDialect, TMode, TResult, PerRow> Except<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.OrderedOperand);
}
