namespace AuraORM;

/// <summary>
/// A SELECT statement with an offset: the last stage.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public sealed class SelectOffset<TDialect, TMode, TResult> : SelectQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    internal SelectOffset(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

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
