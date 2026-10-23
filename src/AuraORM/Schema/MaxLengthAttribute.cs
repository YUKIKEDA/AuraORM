namespace AuraORM;

/// <summary>
/// Sets the maximum length of a <see cref="string"/> or byte array column, used for its type in DDL
/// (<c>docs/design.ja.md</c> §3.3). A key, unique, index, or foreign key column of these types needs it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class MaxLengthAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxLengthAttribute"/> class.
    /// </summary>
    /// <param name="length">The maximum number of characters or bytes.</param>
    public MaxLengthAttribute(int length)
    {
        Length = length;
    }

    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public int Length { get; }
}
