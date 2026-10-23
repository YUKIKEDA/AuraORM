namespace AuraORM;

/// <summary>
/// One parameter of a <see cref="SqlStatement"/>.
/// </summary>
/// <param name="Name">The placeholder as it appears in the SQL text: <c>@p0</c>, or <c>$1</c> on PostgreSQL.</param>
/// <param name="Value">
/// The value sent to the provider: <see langword="null"/> for SQL <c>NULL</c>, and the underlying integer for an enum.
/// </param>
public readonly record struct SqlStatementParameter(string Name, object? Value)
{
    /// <summary>
    /// Gets the type that the parameter is sent with, or <see langword="null"/> to let the provider infer it from the value.
    /// The dialect sets it only where the provider would infer a wrong type (<c>docs/design.ja.md</c> §12.4).
    /// </summary>
    internal System.Data.DbType? DbType { get; init; }
}
