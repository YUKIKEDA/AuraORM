namespace AuraORM;

/// <summary>
/// A SQL dialect. The dialects are a closed set: <see cref="SqliteDialect"/>, <see cref="PostgreSqlDialect"/>,
/// <see cref="MySqlDialect"/>, <see cref="MariaDbDialect"/>, and <see cref="SqlServerDialect"/>.
/// </summary>
/// <remarks>
/// A dialect type declares what its database supports by implementing the capability interfaces (<c>docs/design.ja.md</c> §2.4).
/// An API that needs a capability is available only when the dialect type argument implements it.
/// </remarks>
public interface IDialect;
