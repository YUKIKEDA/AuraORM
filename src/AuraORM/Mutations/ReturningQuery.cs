using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> with <c>Returning</c>: it returns the values of the rows it changed
/// (<c>docs/design.ja.md</c> §9). Run it with the methods of <see cref="QueryExecution"/>; it cannot be a subquery.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <remarks>
/// <para>
/// The order of the rows is not defined. SQLite returns them in an arbitrary order, SQL Server does not guarantee the order
/// of <c>OUTPUT</c>, and AuraORM does not guarantee PostgreSQL's.
/// </para>
/// <para>
/// On SQL Server the statement is written with <c>OUTPUT</c> without <c>INTO</c>, which fails on a table that has an enabled
/// trigger for the same action; AuraORM does not rewrite it to <c>OUTPUT … INTO</c>.
/// </para>
/// <para>
/// A reader closed before the last row (<c>FirstAsync</c>, an enumeration that stops) is not cancelled, unlike a SELECT: on
/// MariaDB and SQL Server a cancel undoes the whole change without an error. The rows left are read and dropped, so
/// <c>FirstAsync</c> returns after all of them have arrived.
/// </para>
/// </remarks>
public sealed class ReturningQuery<TDialect, TMode, TResult> : ResultQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    private readonly MutationStatement<TDialect, TMode> _statement;
    private readonly ImmutableArray<SqlNode> _items;

    internal ReturningQuery(MutationStatement<TDialect, TMode> statement, IProjection<TResult, PerRow> projection)
        : base(statement.Session, (ref RowReader reader) => reader.Read(projection))
    {
        _statement = statement;
        var items = new List<SqlNode>();
        projection.CollectSelectItems(items);
        _items = [.. items];
    }

    /// <inheritdoc />
    internal override bool CancelsUnreadRows => false;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A subquery of <c>Returning</c> reads the table that the statement changes,
    /// or <c>Returning</c> holds a subquery on SQL Server (<c>docs/design.ja.md</c> §9, §14.3).</exception>
    public override SqlStatement ToSql() => SqlRenderContext.Render(DialectRenderer.For<TDialect>(), _statement.CreateNode(_items));
}
