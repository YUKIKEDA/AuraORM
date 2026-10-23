namespace AuraORM;

/// <summary>
/// Gives a column a default value in the database. The column can then be left out of <c>Insert</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class DefaultAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAttribute"/> class.
    /// </summary>
    /// <param name="value">
    /// A <see cref="SqlDefault"/> value, or a constant (a number, a string, or a Boolean) that DDL writes as a literal. There is
    /// one constructor only: with an overload that takes <see cref="SqlDefault"/>, the literal <c>0</c> would convert to the
    /// enum and mean <see cref="SqlDefault.CurrentTimestamp"/>.
    /// </param>
    public DefaultAttribute(object value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the default: a <see cref="SqlDefault"/> or a constant.
    /// </summary>
    public object Value { get; }
}
