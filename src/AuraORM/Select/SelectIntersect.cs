namespace AuraORM;

/// <summary>
/// Statements combined with <c>INTERSECT</c> (<c>docs/design.ja.md</c> §5.10). Only <c>Intersect</c> can add another operand: mixed
/// operators bind differently on SQLite, which evaluates a compound SELECT from left to right.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the first statement's projection.</typeparam>
public sealed class SelectIntersect<TDialect, TMode, TResult, TLevel> : SelectCombination<TDialect, TMode, TResult, TLevel>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TLevel : EvaluationLevel
{
    internal SelectIntersect(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    /// <summary>
    /// Adds another operand: <c>… INTERSECT … INTERSECT …</c>.
    /// </summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TOtherLevel">The evaluation level of the other statement's projection.</typeparam>
    /// <param name="other">The other statement, with the same dialect and result type.</param>
    /// <returns>The set operation.</returns>
    public SelectIntersect<TDialect, TMode, TResult, TLevel> Intersect<TOtherMode, TOtherLevel>(
        SelectOperand<TDialect, TOtherMode, TResult, TOtherLevel> other
    )
        where TOtherMode : ExecutionMode
        where TOtherLevel : EvaluationLevel => new(Session, SelectState.Combine(State, SetOperator.Intersect, other), Factory);

    /// <summary>Not accepted: the other statement has <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c>, or is a set operation.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.RejectedOperand, error: true)]
    public SelectIntersect<TDialect, TMode, TResult, TLevel> Intersect<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.RejectedOperand);

    /// <summary>Not accepted: a chain of set operations holds one operator.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.MixedOperators, error: true)]
    public SelectUnion<TDialect, TMode, TResult, TLevel> Union<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.MixedOperators);

    /// <summary>Not accepted: a chain of set operations holds one operator.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.MixedOperators, error: true)]
    public SelectUnionAll<TDialect, TMode, TResult, TLevel> UnionAll<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.MixedOperators);

    /// <summary>Not accepted: a chain of set operations holds one operator.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(SetOperationMessages.MixedOperators, error: true)]
    public SelectExcept<TDialect, TMode, TResult, TLevel> Except<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(SetOperationMessages.MixedOperators);
}
