namespace AuraORM;

/// <summary>
/// A set operation: statements combined with one operator (<c>docs/design.ja.md</c> §5.10). <c>OrderBy</c>, <c>Limit</c>, and
/// <c>Offset</c> that follow apply to the whole compound SELECT. <c>OrderBy</c> gets the result's items and names each by
/// its position (<see cref="SelectCombinationOrdering"/>).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the first statement's projection, which <c>ORDER BY</c> keys share.</typeparam>
public abstract class SelectCombination<TDialect, TMode, TResult, TLevel> : SelectProjection<TDialect, TMode, TResult, TLevel>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TLevel : EvaluationLevel
{
    private protected SelectCombination(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    internal SelectOrdered<TDialect, TMode, TResult> OrderByCore(Ordering<TLevel>[] orderings)
    {
        ArgumentNullException.ThrowIfNull(orderings);
        if (!State.AllExpressions)
        {
            // §5.10: a whole row or a result type spreads over several columns, so the items have no positions.
            throw new InvalidOperationException(
                "OrderBy after a set operation names the selected items by position, and this projection holds a whole row or a result type. Select a tuple of expressions, or make the set operation a derived table and order it there."
            );
        }

        return new(Session, State.WithOrderBy(orderings), Factory);
    }
}
