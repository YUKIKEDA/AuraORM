namespace AuraORM;

/// <summary>
/// One parameter of a <see cref="SqlStatement"/>.
/// </summary>
/// <param name="Name">The placeholder as it appears in the SQL text: <c>@p0</c>, or <c>$1</c> on PostgreSQL.</param>
/// <param name="Value">
/// The value sent to the provider: <see langword="null"/> for SQL <c>NULL</c>, and the underlying integer for an enum.
/// </param>
public readonly record struct SqlStatementParameter(string Name, object? Value);
