namespace AuraORM;

/// <summary>
/// Settings of a <see cref="Database{TDialect}"/>.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Gets the command timeout in seconds, or <see langword="null"/> for the provider's default.
    /// </summary>
    public int? CommandTimeout { get; init; }
}
