namespace AuraORM;

/// <content>
/// The aggregate functions (<c>docs/design.ja.md</c> §4.2, §4.5). They take <see cref="PerRow"/> expressions and return
/// <see cref="PerGroup"/> expressions, so an aggregate cannot be mixed with row values or nested in another aggregate.
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Counts the rows: <c>COUNT(*)</c>, or <c>COUNT_BIG(*)</c> on SQL Server.
    /// </summary>
    /// <returns>The expression.</returns>
    public static IExpr<long, PerGroup> Count() => Aggregate<long>(AggregateFunction.CountAll, null);

    /// <summary>
    /// Counts the rows where a value is not null: <c>COUNT(a)</c>, or <c>COUNT_BIG(a)</c> on SQL Server.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The counted value.</param>
    /// <returns>The expression.</returns>
    public static IExpr<long, PerGroup> Count<T>(IExpr<T, PerRow> value) => Aggregate<long>(AggregateFunction.Count, value.Node);

    /// <summary>
    /// Counts the distinct non-null values: <c>COUNT(DISTINCT a)</c>, or <c>COUNT_BIG(DISTINCT a)</c> on SQL Server.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The counted value.</param>
    /// <returns>The expression.</returns>
    public static IExpr<long, PerGroup> CountDistinct<T>(IExpr<T, PerRow> value) => Aggregate<long>(AggregateFunction.CountDistinct, value.Node);

    /// <summary>
    /// Sums integers as a 64-bit integer: <c>SUM(a)</c> on SQLite, <c>CAST(SUM(a) AS bigint)</c> on PostgreSQL,
    /// <c>CAST(SUM(a) AS SIGNED)</c> on MySQL and MariaDB, and <c>SUM(CAST(a AS bigint))</c> on SQL Server.
    /// </summary>
    /// <param name="value">The summed value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<long?, PerGroup> Sum(IExpr<int, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<int?, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<long, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<long?, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<short, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<short?, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<byte, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <inheritdoc cref="Sum(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Sum(IExpr<byte?, PerRow> value) => Aggregate<long?>(AggregateFunction.SumInteger, value.Node);

    /// <summary>
    /// Sums numbers: <c>SUM(a)</c>. On SQLite, decimals are stored as text and summed as floating-point numbers.
    /// </summary>
    /// <param name="value">The summed value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<decimal?, PerGroup> Sum(IExpr<decimal, PerRow> value) => DecimalAggregate(AggregateFunction.Sum, value.Node, "SUM");

    /// <inheritdoc cref="Sum(IExpr{decimal, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Sum(IExpr<decimal?, PerRow> value) => DecimalAggregate(AggregateFunction.Sum, value.Node, "SUM");

    /// <inheritdoc cref="Sum(IExpr{decimal, PerRow})"/>
    public static IExpr<double?, PerGroup> Sum(IExpr<double, PerRow> value) => Aggregate<double?>(AggregateFunction.Sum, value.Node);

    /// <inheritdoc cref="Sum(IExpr{decimal, PerRow})"/>
    public static IExpr<double?, PerGroup> Sum(IExpr<double?, PerRow> value) => Aggregate<double?>(AggregateFunction.Sum, value.Node);

    /// <inheritdoc cref="Sum(IExpr{decimal, PerRow})"/>
    public static IExpr<float?, PerGroup> Sum(IExpr<float, PerRow> value) => Aggregate<float?>(AggregateFunction.Sum, value.Node);

    /// <inheritdoc cref="Sum(IExpr{decimal, PerRow})"/>
    public static IExpr<float?, PerGroup> Sum(IExpr<float?, PerRow> value) => Aggregate<float?>(AggregateFunction.Sum, value.Node);

    /// <summary>
    /// Averages numbers as a double: <c>AVG(a)</c> on SQLite, <c>AVG(CAST(a AS double precision))</c> on PostgreSQL,
    /// <c>CAST(AVG(a) AS DOUBLE)</c> on MySQL and MariaDB, and <c>AVG(CAST(a AS float))</c> on SQL Server.
    /// </summary>
    /// <param name="value">The averaged value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<double?, PerGroup> Avg(IExpr<int, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<int?, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<long, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<long?, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<short, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<short?, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<byte, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<byte?, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<double, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<double?, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<float, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <inheritdoc cref="Avg(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Avg(IExpr<float?, PerRow> value) => Aggregate<double?>(AggregateFunction.AverageAsDouble, value.Node);

    /// <summary>
    /// Averages decimals: <c>AVG(a)</c>.
    /// </summary>
    /// <param name="value">The averaged value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<decimal?, PerGroup> Avg(IExpr<decimal, PerRow> value) => DecimalAggregate(AggregateFunction.Average, value.Node, "AVG");

    /// <inheritdoc cref="Avg(IExpr{decimal, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Avg(IExpr<decimal?, PerRow> value) => DecimalAggregate(AggregateFunction.Average, value.Node, "AVG");

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<byte?, PerGroup> Min(IExpr<byte, PerRow> value) => Aggregate<byte?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<byte?, PerGroup> Min(IExpr<byte?, PerRow> value) => Aggregate<byte?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<short?, PerGroup> Min(IExpr<short, PerRow> value) => Aggregate<short?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<short?, PerGroup> Min(IExpr<short?, PerRow> value) => Aggregate<short?>(AggregateFunction.Min, value.Node);

    /// <summary>
    /// Returns the smallest value: <c>MIN(a)</c>. The value is a number, a date or time, a string, or an enum (by its
    /// underlying integer); <see cref="bool"/> and <see cref="Guid"/> have no order on every dialect.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<int?, PerGroup> Min(IExpr<int, PerRow> value) => Aggregate<int?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<int?, PerGroup> Min(IExpr<int?, PerRow> value) => Aggregate<int?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Min(IExpr<long, PerRow> value) => Aggregate<long?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Min(IExpr<long?, PerRow> value) => Aggregate<long?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<float?, PerGroup> Min(IExpr<float, PerRow> value) => Aggregate<float?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<float?, PerGroup> Min(IExpr<float?, PerRow> value) => Aggregate<float?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Min(IExpr<double, PerRow> value) => Aggregate<double?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Min(IExpr<double?, PerRow> value) => Aggregate<double?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Min(IExpr<decimal, PerRow> value) => DecimalAggregate(AggregateFunction.Min, value.Node, "MIN");

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Min(IExpr<decimal?, PerRow> value) => DecimalAggregate(AggregateFunction.Min, value.Node, "MIN");

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<DateTime?, PerGroup> Min(IExpr<DateTime, PerRow> value) => Aggregate<DateTime?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<DateTime?, PerGroup> Min(IExpr<DateTime?, PerRow> value) => Aggregate<DateTime?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<DateTimeOffset?, PerGroup> Min(IExpr<DateTimeOffset, PerRow> value) =>
        Aggregate<DateTimeOffset?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<DateTimeOffset?, PerGroup> Min(IExpr<DateTimeOffset?, PerRow> value) =>
        Aggregate<DateTimeOffset?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<DateOnly?, PerGroup> Min(IExpr<DateOnly, PerRow> value) => Aggregate<DateOnly?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<DateOnly?, PerGroup> Min(IExpr<DateOnly?, PerRow> value) => Aggregate<DateOnly?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<TimeOnly?, PerGroup> Min(IExpr<TimeOnly, PerRow> value) => Aggregate<TimeOnly?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<TimeOnly?, PerGroup> Min(IExpr<TimeOnly?, PerRow> value) => Aggregate<TimeOnly?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min(IExpr{int, PerRow})"/>
    public static IExpr<string?, PerGroup> Min(IExpr<string?, PerRow> value) => Aggregate<string?>(AggregateFunction.Min, value.Node);

    /// <summary>
    /// Returns the smallest enum value by its underlying integer: <c>MIN(a)</c>.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<T?, PerGroup> Min<T>(IExpr<T, PerRow> value)
        where T : struct, Enum => Aggregate<T?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min{T}(IExpr{T, PerRow})"/>
    public static IExpr<T?, PerGroup> Min<T>(IExpr<T?, PerRow> value)
        where T : struct, Enum => Aggregate<T?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<byte?, PerGroup> Max(IExpr<byte, PerRow> value) => Aggregate<byte?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<byte?, PerGroup> Max(IExpr<byte?, PerRow> value) => Aggregate<byte?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<short?, PerGroup> Max(IExpr<short, PerRow> value) => Aggregate<short?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<short?, PerGroup> Max(IExpr<short?, PerRow> value) => Aggregate<short?>(AggregateFunction.Max, value.Node);

    /// <summary>
    /// Returns the largest value: <c>MAX(a)</c>. The value is a number, a date or time, a string, or an enum (by its
    /// underlying integer); <see cref="bool"/> and <see cref="Guid"/> have no order on every dialect.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<int?, PerGroup> Max(IExpr<int, PerRow> value) => Aggregate<int?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<int?, PerGroup> Max(IExpr<int?, PerRow> value) => Aggregate<int?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Max(IExpr<long, PerRow> value) => Aggregate<long?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<long?, PerGroup> Max(IExpr<long?, PerRow> value) => Aggregate<long?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<float?, PerGroup> Max(IExpr<float, PerRow> value) => Aggregate<float?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<float?, PerGroup> Max(IExpr<float?, PerRow> value) => Aggregate<float?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Max(IExpr<double, PerRow> value) => Aggregate<double?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<double?, PerGroup> Max(IExpr<double?, PerRow> value) => Aggregate<double?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Max(IExpr<decimal, PerRow> value) => DecimalAggregate(AggregateFunction.Max, value.Node, "MAX");

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Max(IExpr<decimal?, PerRow> value) => DecimalAggregate(AggregateFunction.Max, value.Node, "MAX");

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<DateTime?, PerGroup> Max(IExpr<DateTime, PerRow> value) => Aggregate<DateTime?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<DateTime?, PerGroup> Max(IExpr<DateTime?, PerRow> value) => Aggregate<DateTime?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<DateTimeOffset?, PerGroup> Max(IExpr<DateTimeOffset, PerRow> value) =>
        Aggregate<DateTimeOffset?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<DateTimeOffset?, PerGroup> Max(IExpr<DateTimeOffset?, PerRow> value) =>
        Aggregate<DateTimeOffset?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<DateOnly?, PerGroup> Max(IExpr<DateOnly, PerRow> value) => Aggregate<DateOnly?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<DateOnly?, PerGroup> Max(IExpr<DateOnly?, PerRow> value) => Aggregate<DateOnly?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<TimeOnly?, PerGroup> Max(IExpr<TimeOnly, PerRow> value) => Aggregate<TimeOnly?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<TimeOnly?, PerGroup> Max(IExpr<TimeOnly?, PerRow> value) => Aggregate<TimeOnly?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max(IExpr{int, PerRow})"/>
    public static IExpr<string?, PerGroup> Max(IExpr<string?, PerRow> value) => Aggregate<string?>(AggregateFunction.Max, value.Node);

    /// <summary>
    /// Returns the largest enum value by its underlying integer: <c>MAX(a)</c>.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<T?, PerGroup> Max<T>(IExpr<T, PerRow> value)
        where T : struct, Enum => Aggregate<T?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max{T}(IExpr{T, PerRow})"/>
    public static IExpr<T?, PerGroup> Max<T>(IExpr<T?, PerRow> value)
        where T : struct, Enum => Aggregate<T?>(AggregateFunction.Max, value.Node);

    private static SqlExpr<T, PerGroup> Aggregate<T>(AggregateFunction function, SqlNode? argument) => new(new AggregateNode(function, argument));

    // MIN / MAX / SUM / AVG of decimals are rejected on SQLite (§3.3).
    private static SqlExpr<decimal?, PerGroup> DecimalAggregate(AggregateFunction function, SqlNode argument, string name) =>
        new(new DecimalOrderingNode(new AggregateNode(function, argument), name));
}
