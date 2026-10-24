using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// What a SELECT statement holds. Each builder call makes a new state, so a stage can be reused without changing the
/// statements built from it.
/// </summary>
internal sealed record SelectState(
    ImmutableArray<SelectSourceState> Sources,
    SqlNode? Where,
    bool Distinct,
    ImmutableArray<SqlNode> Items,
    ImmutableArray<OrderKey> OrderBy,
    ParameterNode? Limit,
    ParameterNode? Offset
)
{
    /// <summary>
    /// Gets the keys of <c>GROUP BY</c>, or an empty array when the statement has none.
    /// </summary>
    public ImmutableArray<SqlNode> GroupBy { get; init; } = ImmutableArray<SqlNode>.Empty;

    /// <summary>
    /// Gets the condition of <c>HAVING</c>, or <see langword="null"/> when the statement has none.
    /// </summary>
    public SqlNode? Having { get; init; }

    /// <summary>
    /// Gets a value indicating whether the projection is one expression, which a scalar subquery needs (§5.11).
    /// </summary>
    public bool SingleExpression { get; init; }

    /// <summary>
    /// Gets a value indicating whether every item of the projection is an expression (no whole row, no result type), so that
    /// the items have positions in the select list (§5.10).
    /// </summary>
    public bool AllExpressions { get; init; }

    /// <summary>
    /// Gets the statements combined with this one by a set operation, in order, or an empty array (§5.10).
    /// </summary>
    public ImmutableArray<SetOperand> SetOperands { get; init; } = ImmutableArray<SetOperand>.Empty;

    /// <summary>
    /// Gets the named queries that the statement writes as CTEs, in order, or an empty array (§5.11).
    /// </summary>
    public ImmutableArray<NamedQueryDefinition> Ctes { get; init; } = ImmutableArray<NamedQueryDefinition>.Empty;

    /// <summary>
    /// Adds an operand of a set operation.
    /// </summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <typeparam name="TMode">The other statement's mode.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TLevel">The other statement's level.</typeparam>
    /// <param name="state">The set operation so far, or its first statement.</param>
    /// <param name="setOperator">The operator.</param>
    /// <param name="other">The other statement.</param>
    /// <returns>The state of the set operation.</returns>
    public static SelectState Combine<TDialect, TMode, TResult, TLevel>(
        SelectState state,
        SetOperator setOperator,
        SelectOperand<TDialect, TMode, TResult, TLevel> other
    )
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(other);
        return state with { SetOperands = state.SetOperands.Add(new SetOperand(setOperator, other.State)) };
    }

    public static SelectState From(TableAlias table) =>
        new(
            [new SelectSourceState(table, JoinKind.From, null)],
            null,
            false,
            ImmutableArray<SqlNode>.Empty,
            ImmutableArray<OrderKey>.Empty,
            null,
            null
        );

    /// <summary>
    /// Creates the occurrence of a table that a join adds. A table that the statement already has under the same name (its
    /// alias, or its name when it has none) needs an alias (<c>docs/design.ja.md</c> §5.2, §14.3).
    /// </summary>
    /// <typeparam name="TColumns">The column set.</typeparam>
    /// <typeparam name="TNullable">The nullable column set.</typeparam>
    /// <param name="table">The table or named query.</param>
    /// <returns>The occurrence.</returns>
    /// <exception cref="InvalidOperationException">The statement already has a table under the same name.</exception>
    public TableAlias NewSource<TColumns, TNullable>(ITableSource<TColumns, TNullable> table)
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable>
    {
        ArgumentNullException.ThrowIfNull(table);
        TableAlias alias = table.CreateOccurrence();
        foreach (SelectSourceState source in Sources)
        {
            if (source.Table.Reference == alias.Reference)
            {
                throw new InvalidOperationException(
                    "The statement already has a table named " + alias.Reference + ". Give the second use of the table an alias with As(\"...\")."
                );
            }
        }

        return alias;
    }

    public SelectState Join(TableAlias table, JoinKind kind, SqlNode? on) =>
        this with
        {
            Sources = Sources.Add(new SelectSourceState(table, kind, on)),
        };

    public SelectState WithProjection<T, TLevel>(IProjection<T, TLevel> projection, bool distinct)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(projection);
        var items = new List<SqlNode>();
        projection.CollectSelectItems(items);
        bool single = projection is IExpr<T, TLevel>;
        return this with
        {
            Items = [.. items],
            Distinct = distinct,
            SingleExpression = single,
            AllExpressions = single || projection is IExpressionTuple { AllExpressions: true },
        };
    }

    public SelectState WithOrderBy<TLevel>(Ordering<TLevel>[] orderings)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(orderings);
        if (orderings.Length == 0)
        {
            throw new ArgumentException("ORDER BY needs at least one key.", nameof(orderings));
        }

        var keys = ImmutableArray.CreateBuilder<OrderKey>(orderings.Length);
        foreach (Ordering<TLevel> ordering in orderings)
        {
            ArgumentNullException.ThrowIfNull(ordering, nameof(orderings));
            keys.Add(ordering.Key);
        }

        return this with
        {
            OrderBy = keys.MoveToImmutable(),
        };
    }

    public static ParameterNode RowLimit<TDialect>(int value, string paramName)
        where TDialect : IDialect
    {
        int minimum = DialectRenderer.For<TDialect>().MinimumRowLimit;
        if (value < minimum)
        {
            ThrowHelper.ThrowArgumentOutOfRange(
                paramName,
                minimum == 0 ? "The number of rows cannot be negative." : "SQL Server's FETCH needs a limit of at least one row."
            );
        }

        return new ParameterNode<int>(value);
    }

    public static ParameterNode RowCount(int value, string paramName)
    {
        if (value < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRange(paramName, "The number of rows cannot be negative.");
        }

        return new ParameterNode<int>(value);
    }
}
