namespace AuraORM;

/// <summary>
/// Declares a foreign key: the column refers to a column of another table. The generator checks that the target is a
/// table, that the member is a column, and that the types match.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class ReferencesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferencesAttribute"/> class.
    /// </summary>
    /// <param name="table">The row type of the referenced table.</param>
    /// <param name="member">The referenced member, written with <c>nameof</c>.</param>
    public ReferencesAttribute(Type table, string member)
    {
        Table = table;
        Member = member;
    }

    /// <summary>
    /// Gets the row type of the referenced table.
    /// </summary>
    public Type Table { get; }

    /// <summary>
    /// Gets the referenced member.
    /// </summary>
    public string Member { get; }
}
