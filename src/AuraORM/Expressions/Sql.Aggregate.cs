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
    public static IExpr<decimal?, PerGroup> Sum(IExpr<decimal, PerRow> value) => Aggregate<decimal?>(AggregateFunction.Sum, value.Node);

    /// <inheritdoc cref="Sum(IExpr{decimal, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Sum(IExpr<decimal?, PerRow> value) => Aggregate<decimal?>(AggregateFunction.Sum, value.Node);

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
    public static IExpr<decimal?, PerGroup> Avg(IExpr<decimal, PerRow> value) => Aggregate<decimal?>(AggregateFunction.Average, value.Node);

    /// <inheritdoc cref="Avg(IExpr{decimal, PerRow})"/>
    public static IExpr<decimal?, PerGroup> Avg(IExpr<decimal?, PerRow> value) => Aggregate<decimal?>(AggregateFunction.Average, value.Node);

    /// <summary>
    /// Returns the smallest value: <c>MIN(a)</c>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<T?, PerGroup> Min<T>(IExpr<T, PerRow> value)
        where T : struct => Aggregate<T?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min{T}(IExpr{T, PerRow})"/>
    public static IExpr<T?, PerGroup> Min<T>(IExpr<T?, PerRow> value)
        where T : struct => Aggregate<T?>(AggregateFunction.Min, value.Node);

    /// <inheritdoc cref="Min{T}(IExpr{T, PerRow})"/>
    public static IExpr<string?, PerGroup> Min(IExpr<string?, PerRow> value) => Aggregate<string?>(AggregateFunction.Min, value.Node);

    /// <summary>
    /// Returns the largest value: <c>MAX(a)</c>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The expression. It is null when there are no non-null values.</returns>
    public static IExpr<T?, PerGroup> Max<T>(IExpr<T, PerRow> value)
        where T : struct => Aggregate<T?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max{T}(IExpr{T, PerRow})"/>
    public static IExpr<T?, PerGroup> Max<T>(IExpr<T?, PerRow> value)
        where T : struct => Aggregate<T?>(AggregateFunction.Max, value.Node);

    /// <inheritdoc cref="Max{T}(IExpr{T, PerRow})"/>
    public static IExpr<string?, PerGroup> Max(IExpr<string?, PerRow> value) => Aggregate<string?>(AggregateFunction.Max, value.Node);

    private static SqlExpr<T, PerGroup> Aggregate<T>(AggregateFunction function, SqlNode? argument) => new(new AggregateNode(function, argument));
}
