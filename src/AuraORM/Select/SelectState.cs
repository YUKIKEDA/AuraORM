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
    /// <param name="table">The table reference.</param>
    /// <returns>The occurrence.</returns>
    /// <exception cref="InvalidOperationException">The statement already has a table under the same name.</exception>
    public TableAlias NewSource(ITableReference table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var alias = new TableAlias(table.Info.Schema, table.Info.Name, table.Alias);
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
