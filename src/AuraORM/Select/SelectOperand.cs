namespace AuraORM;

/// <summary>
/// A SELECT statement right after its projection that can be an operand of a set operation (<c>docs/design.ja.md</c> §5.10):
/// the projection of one or more tables, of groups, or of <c>SELECT DISTINCT</c>. A statement with <c>OrderBy</c>,
/// <c>Limit</c>, or <c>Offset</c>, and a set operation, are not operands: SQLite does not accept an operand in parentheses, so an
/// operand cannot carry those clauses, and a chain holds one operator.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
public abstract class SelectOperand<TDialect, TMode, TResult, TLevel> : SelectProjection<TDialect, TMode, TResult, TLevel>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TLevel : EvaluationLevel
{
    private protected const string RejectedOperand =
        "A set operation's operand is a statement right after its projection: an operand with OrderBy, Limit, or Offset is not accepted (SQLite rejects them inside a compound SELECT), nor is another set operation (mixed operators read differently on SQLite); order or limit the result of the set operation, or make the operand a derived table.";

    private protected SelectOperand(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    /// <summary>
    /// Combines this statement with another: <c>… UNION …</c>, the rows of either statement, without duplicates. The other statement has the same dialect and result
    /// type; it may be built to run or not, and its projection may have either evaluation level.
    /// </summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TOtherLevel">The evaluation level of the other statement's projection.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>The set operation, to which only <c>Union</c> can add another operand.</returns>
    public SelectUnion<TDialect, TMode, TResult, TLevel> Union<TOtherMode, TOtherLevel>(
        SelectOperand<TDialect, TOtherMode, TResult, TOtherLevel> other
    )
        where TOtherMode : ExecutionMode
        where TOtherLevel : EvaluationLevel => new(Session, SelectState.Combine(State, SetOperator.Union, other), Factory);

    /// <summary>Not accepted: the other statement has <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c>, or is a set operation.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(RejectedOperand, error: true)]
    public SelectUnion<TDialect, TMode, TResult, TLevel> Union<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(RejectedOperand);

    /// <summary>
    /// Combines this statement with another: <c>… UNION ALL …</c>, the rows of both statements, with duplicates. The other statement has the same dialect and result
    /// type; it may be built to run or not, and its projection may have either evaluation level.
    /// </summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TOtherLevel">The evaluation level of the other statement's projection.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>The set operation, to which only <c>UnionAll</c> can add another operand.</returns>
    public SelectUnionAll<TDialect, TMode, TResult, TLevel> UnionAll<TOtherMode, TOtherLevel>(
        SelectOperand<TDialect, TOtherMode, TResult, TOtherLevel> other
    )
        where TOtherMode : ExecutionMode
        where TOtherLevel : EvaluationLevel => new(Session, SelectState.Combine(State, SetOperator.UnionAll, other), Factory);

    /// <summary>Not accepted: the other statement has <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c>, or is a set operation.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(RejectedOperand, error: true)]
    public SelectUnionAll<TDialect, TMode, TResult, TLevel> UnionAll<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(RejectedOperand);

    /// <summary>
    /// Combines this statement with another: <c>… INTERSECT …</c>, the rows that both statements return. The other statement has the same dialect and result
    /// type; it may be built to run or not, and its projection may have either evaluation level.
    /// </summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TOtherLevel">The evaluation level of the other statement's projection.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>The set operation, to which only <c>Intersect</c> can add another operand.</returns>
    public SelectIntersect<TDialect, TMode, TResult, TLevel> Intersect<TOtherMode, TOtherLevel>(
        SelectOperand<TDialect, TOtherMode, TResult, TOtherLevel> other
    )
        where TOtherMode : ExecutionMode
        where TOtherLevel : EvaluationLevel => new(Session, SelectState.Combine(State, SetOperator.Intersect, other), Factory);

    /// <summary>Not accepted: the other statement has <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c>, or is a set operation.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(RejectedOperand, error: true)]
    public SelectIntersect<TDialect, TMode, TResult, TLevel> Intersect<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(RejectedOperand);

    /// <summary>
    /// Combines this statement with another: <c>… EXCEPT …</c>, the rows of this statement that the other does not return. The other statement has the same dialect and result
    /// type; it may be built to run or not, and its projection may have either evaluation level.
    /// </summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TOtherLevel">The evaluation level of the other statement's projection.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>The set operation, to which only <c>Except</c> can add another operand.</returns>
    public SelectExcept<TDialect, TMode, TResult, TLevel> Except<TOtherMode, TOtherLevel>(
        SelectOperand<TDialect, TOtherMode, TResult, TOtherLevel> other
    )
        where TOtherMode : ExecutionMode
        where TOtherLevel : EvaluationLevel => new(Session, SelectState.Combine(State, SetOperator.Except, other), Factory);

    /// <summary>Not accepted: the other statement has <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c>, or is a set operation.</summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run.</typeparam>
    /// <param name="other">The other statement.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(RejectedOperand, error: true)]
    public SelectExcept<TDialect, TMode, TResult, TLevel> Except<TOtherMode>(SelectQuery<TDialect, TOtherMode, TResult> other)
        where TOtherMode : ExecutionMode => throw new InvalidOperationException(RejectedOperand);
}
