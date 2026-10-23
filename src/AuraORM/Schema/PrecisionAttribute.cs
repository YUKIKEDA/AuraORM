namespace AuraORM;

/// <summary>
/// Sets the precision and scale of a <see cref="decimal"/> column, used for its type in DDL. Without it, a decimal column
/// is <c>(18, 2)</c> (<c>docs/design.ja.md</c> §3.3).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class PrecisionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrecisionAttribute"/> class.
    /// </summary>
    /// <param name="precision">The total number of digits.</param>
    /// <param name="scale">The number of digits after the decimal point.</param>
    public PrecisionAttribute(int precision, int scale)
    {
        Precision = precision;
        Scale = scale;
    }

    /// <summary>
    /// Gets the total number of digits.
    /// </summary>
    public int Precision { get; }

    /// <summary>
    /// Gets the number of digits after the decimal point.
    /// </summary>
    public int Scale { get; }
}
