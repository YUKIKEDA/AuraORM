namespace AuraORM;

/// <summary>
/// A <c>SELECT DISTINCT</c> of an expression or a tuple of expressions (<c>docs/design.ja.md</c> §5.7). Its <c>OrderBy</c>
/// gets the selected items, not the tables' columns, because PostgreSQL and SQL Server accept only items of the select list
/// as <c>ORDER BY</c> keys of a <c>SELECT DISTINCT</c>.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
/// <typeparam name="TItems">The selected items as the <c>OrderBy</c> lambda gets them: a <see cref="DistinctItem{T, TLevel}"/>,
/// or a tuple of them.</typeparam>
public sealed class SelectDistinctProjected<TDialect, TMode, TResult, TLevel, TItems> : SelectProjection<TDialect, TMode, TResult, TLevel>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TLevel : EvaluationLevel
{
    private readonly TItems _items;

    internal SelectDistinctProjected(IStatementRunner? session, SelectState state, IProjection<TResult, TLevel> projection, TItems items)
        : base(session, state, FactoryOf(projection))
    {
        _items = items;
    }

    /// <summary>
    /// Orders the rows by selected items: <c>ORDER BY 1 ASC, 2 DESC</c>, each key the position of its item in the select list
    /// (<c>docs/design.ja.md</c> §5.7).
    /// </summary>
    /// <param name="orderings">The keys, in order, from the items: <c>s =&gt; [Asc(s.Item1), Desc(s.Item2)]</c>.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException">There is no key.</exception>
    public SelectOrdered<TDialect, TMode, TResult> OrderBy(Func<TItems, Ordering<TLevel>[]> orderings)
    {
        ArgumentNullException.ThrowIfNull(orderings);
        return new(Session, State.WithOrderBy(orderings(_items)), Factory);
    }
}
