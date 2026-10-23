namespace AuraORM;

/// <summary>
/// The row limits and offsets that only some dialects accept at a stage (<c>docs/design.ja.md</c> §5.9). On another
/// dialect the method does not exist for the stage, so the call does not compile.
/// </summary>
public static class SelectExtensions
{
    /// <summary>
    /// Limits the number of rows without <c>ORDER BY</c>: <c>LIMIT n</c>. SQL Server needs <c>ORDER BY</c> for a limit, so it
    /// has no such method.
    /// </summary>
    /// <typeparam name="TDialect">A dialect that accepts a limit without <c>ORDER BY</c>.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
    /// <param name="query">The statement to limit.</param>
    /// <param name="count">The largest number of rows. It is a parameter.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static SelectLimited<TDialect, TMode, TResult> Limit<TDialect, TMode, TResult, TLevel>(
        this SelectProjection<TDialect, TMode, TResult, TLevel> query,
        int count
    )
        where TDialect : IUnorderedLimitDialect
        where TMode : ExecutionMode
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.LimitCore(count);
    }

    /// <summary>
    /// Skips rows with neither <c>ORDER BY</c> nor a limit: <c>OFFSET m</c>. Only PostgreSQL accepts it: SQL Server's
    /// <c>OFFSET</c> is part of <c>ORDER BY</c>, and SQLite and MySQL need a limit for an offset.
    /// </summary>
    /// <typeparam name="TDialect">A dialect that accepts an offset with neither <c>ORDER BY</c> nor a limit.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
    /// <param name="query">The statement whose rows to skip.</param>
    /// <param name="count">The number of rows to skip. It is a parameter.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static SelectOffset<TDialect, TMode, TResult> Offset<TDialect, TMode, TResult, TLevel>(
        this SelectProjection<TDialect, TMode, TResult, TLevel> query,
        int count
    )
        where TDialect : IUnorderedOffsetDialect
        where TMode : ExecutionMode
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.OffsetCore(count);
    }

    /// <summary>
    /// Skips rows after <c>ORDER BY</c> without a limit: <c>OFFSET m</c> on PostgreSQL, <c>OFFSET m ROWS</c> on SQL Server.
    /// SQLite and MySQL need a limit for an offset, so they have no such method.
    /// </summary>
    /// <typeparam name="TDialect">A dialect that accepts an offset after <c>ORDER BY</c> without a limit.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TResult">The type that each row is read as.</typeparam>
    /// <param name="query">The statement whose rows to skip.</param>
    /// <param name="count">The number of rows to skip. It is a parameter.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static SelectOffset<TDialect, TMode, TResult> Offset<TDialect, TMode, TResult>(
        this SelectOrdered<TDialect, TMode, TResult> query,
        int count
    )
        where TDialect : IOffsetWithoutLimitDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.OffsetCore(count);
    }
}
