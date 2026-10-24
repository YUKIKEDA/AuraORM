namespace AuraORM;

/// <summary>
/// Statements combined with <c>EXCEPT</c> (<c>docs/design.ja.md</c> §5.10). Only <c>Except</c> can add another operand: mixed
/// operators bind differently on SQLite, which evaluates a compound SELECT from left to right.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the first statement's projection.</typeparam>
public sealed class SelectExcept<TDialect, TMode, TResult, TLevel> : SelectCombination<TDialect, TMode, TResult, TLevel>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TLevel : EvaluationLevel
{
    private const string RejectedOperand =
        "A set operation's operand is a statement right after its projection: an operand with OrderBy, Limit, or Offset is not accepted (SQLite rejects them inside a compound SELECT), nor is another set operation (mixed operators read differently on SQLite); order or limit the result of the set operation, or make the operand a derived table.";

    internal SelectExcept(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    /// <summary>
    /// Adds another operand: <c>… EXCEPT … EXCEPT …</c>.
    /// </summary>
    /// <typeparam name="TOtherMode">Whether the other statement was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TOtherLevel">The evaluation level of the other statement's projection.</typeparam>
    /// <param name="other">The other statement, with the same dialect and result type.</param>
    /// <returns>The set operation.</returns>
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
