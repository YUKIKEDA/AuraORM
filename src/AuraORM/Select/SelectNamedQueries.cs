using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// The <c>As</c> calls that do not compile (<c>docs/design.ja.md</c> §5.11). The source generator writes <c>As</c> for the
/// queries whose rows are a <c>[QueryResult]</c> type or a <c>[Table]</c> row; those are more specific than these, so these
/// catch the others and fail with CS0619 and a message that says how to fix the call.
/// </summary>
public static class SelectNamedQueries
{
    /// <summary>
    /// The message for a query whose rows are an expression or a tuple.
    /// </summary>
    internal const string UnnamedItems =
        "Only a query whose rows are a [QueryResult] type or a [Table] row can be named, because its columns need names; select a result type's projection (new MyResult.RowProjection { ... }) or the whole row.";

    /// <summary>
    /// The message for a query with <c>OrderBy</c> but neither <c>Limit</c> nor <c>Offset</c>.
    /// </summary>
    internal const string OrderedQuery =
        "A named query's ORDER BY has no effect without Limit or Offset, and SQL Server rejects it (error 1033); remove OrderBy, or add Limit.";

    /// <summary>Not accepted: the rows of the query are not a result type or a table row.</summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TResult">The type of the rows.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="name">The name.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(UnnamedItems, error: true)]
    public static NamedQuery As<TDialect, TMode, TResult, TLevel>(this SelectProjection<TDialect, TMode, TResult, TLevel> query, string name)
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TLevel : EvaluationLevel => throw new InvalidOperationException(UnnamedItems);

    /// <inheritdoc cref="As{TDialect, TMode, TResult, TLevel}(SelectProjection{TDialect, TMode, TResult, TLevel}, string)"/>
    [Obsolete(UnnamedItems, error: true)]
    public static NamedQuery As<TDialect, TMode, TResult>(this SelectLimited<TDialect, TMode, TResult> query, string name)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(UnnamedItems);

    /// <inheritdoc cref="As{TDialect, TMode, TResult, TLevel}(SelectProjection{TDialect, TMode, TResult, TLevel}, string)"/>
    [Obsolete(UnnamedItems, error: true)]
    public static NamedQuery As<TDialect, TMode, TResult>(this SelectOffset<TDialect, TMode, TResult> query, string name)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(UnnamedItems);

    /// <summary>Not accepted: a query with <c>OrderBy</c> needs <c>Limit</c> or <c>Offset</c>.</summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TResult">The type of the rows.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="name">The name.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(OrderedQuery, error: true)]
    public static NamedQuery As<TDialect, TMode, TResult>(this SelectOrdered<TDialect, TMode, TResult> query, string name)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedQuery);

    /// <summary>
    /// Creates a named query. The <c>As</c> methods that the source generator writes call this; write <c>query.As("name")</c>
    /// instead.
    /// </summary>
    /// <typeparam name="TColumns">The column set of its rows.</typeparam>
    /// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
    /// <typeparam name="TDialect">The dialect of the query.</typeparam>
    /// <typeparam name="TMode">Whether the query was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TResult">The type of the query's rows.</typeparam>
    /// <param name="query">The query, right after its projection, a limit, or an offset.</param>
    /// <param name="name">The name.</param>
    /// <param name="columnNames">The names of the selected items, in order: the result type's members or the table's
    /// columns.</param>
    /// <returns>The named query.</returns>
    /// <exception cref="ArgumentException">The name is empty, or the query has <c>OrderBy</c> without <c>Limit</c> or
    /// <c>Offset</c>.</exception>
    /// <exception cref="InvalidOperationException">The query has <c>With</c>, which only the outermost statement can
    /// have.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static NamedQuery<TColumns, TNullable> Create<TColumns, TNullable, TDialect, TMode, TResult>(
        SelectQuery<TDialect, TMode, TResult> query,
        string name,
        string[] columnNames
    )
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable>
        where TDialect : IDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(columnNames);
        SelectState state = query.State;
        if (!state.OrderBy.IsEmpty && state.Limit is null && state.Offset is null)
        {
            throw new ArgumentException(OrderedQuery, nameof(query));
        }

        if (!state.Ctes.IsEmpty)
        {
            // §5.11: SQL Server rejects a WITH inside a derived table or a CTE.
            throw new InvalidOperationException(
                "A statement with With cannot be a named query: only the outermost statement has a WITH. List all the named queries in its With."
            );
        }

        return new NamedQuery<TColumns, TNullable>(new NamedQueryDefinition(name, state, DialectRenderer.For<TDialect>(), [.. columnNames]));
    }
}
