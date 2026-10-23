namespace AuraORM;

/// <summary>
/// The target types of <see cref="Sql.Cast{TSource, TTarget, TLevel}(IExpr{TSource, TLevel}, CastType{TTarget})"/>
/// (<c>docs/design.ja.md</c> §4.3).
/// </summary>
/// <remarks>
/// Only these types can be cast to, because MySQL's <c>CAST</c> accepts a short list of types.
/// </remarks>
public static class SqlType
{
    /// <summary>
    /// Gets the 64-bit integer type, read as <see cref="long"/>.
    /// </summary>
    public static CastType<long> Int64 { get; } = new(CastTarget.Int64);

    /// <summary>
    /// Gets the double-precision floating-point type, read as <see cref="double"/>.
    /// </summary>
    public static CastType<double> Double { get; } = new(CastTarget.Double);

    /// <summary>
    /// Gets the exact numeric type, read as <see cref="decimal"/>.
    /// </summary>
    public static CastType<decimal> Decimal { get; } = new(CastTarget.Decimal);

    /// <summary>
    /// Gets the text type, read as <see cref="string"/>.
    /// </summary>
    public static CastType<string> String { get; } = new(CastTarget.String);

    /// <summary>
    /// Gets the date and time type, read as <see cref="System.DateTime"/>.
    /// </summary>
    public static CastType<DateTime> DateTime { get; } = new(CastTarget.DateTime);

    /// <summary>
    /// Gets the date type, read as <see cref="System.DateOnly"/>.
    /// </summary>
    public static CastType<DateOnly> DateOnly { get; } = new(CastTarget.DateOnly);
}
