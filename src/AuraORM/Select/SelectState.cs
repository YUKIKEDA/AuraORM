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
    public static SelectState From(TableAlias table) =>
        new([new SelectSourceState(table)], null, false, ImmutableArray<SqlNode>.Empty, ImmutableArray<OrderKey>.Empty, null, null);

    public SelectState WithProjection<T, TLevel>(IProjection<T, TLevel> projection, bool distinct)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(projection);
        var items = new List<SqlNode>();
        projection.CollectSelectItems(items);
        return this with { Items = [.. items], Distinct = distinct };
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
