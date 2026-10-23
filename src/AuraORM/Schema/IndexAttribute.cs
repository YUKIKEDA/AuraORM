namespace AuraORM;

/// <summary>
/// Declares an index of a table. Name the members with <c>nameof</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class IndexAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexAttribute"/> class.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="members">The members whose columns the index holds, in order.</param>
    public IndexAttribute(string name, params string[] members)
    {
        Name = name;
        Members = members;
    }

    /// <summary>
    /// Gets the index name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the members whose columns the index holds.
    /// </summary>
    public IReadOnlyList<string> Members { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is unique.
    /// </summary>
    public bool Unique { get; set; }
}
