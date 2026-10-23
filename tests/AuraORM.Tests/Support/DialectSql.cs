namespace AuraORM.Tests;

/// <summary>
/// The SQL text that each dialect is expected to render, written by hand from docs/design.ja.md.
/// </summary>
/// <param name="Sqlite">SQLite.</param>
/// <param name="PostgreSql">PostgreSQL.</param>
/// <param name="MySql">MySQL, and MariaDB when <paramref name="MariaDb"/> is not given.</param>
/// <param name="SqlServer">SQL Server.</param>
/// <param name="MariaDb">MariaDB, when it differs from MySQL.</param>
public sealed record DialectSql(string Sqlite, string PostgreSql, string MySql, string SqlServer, string? MariaDb = null)
{
    /// <summary>
    /// Gets the parameter values on SQL Server, when they differ from the other dialects' (a value written twice).
    /// </summary>
    public object?[]? SqlServerParameters { get; init; }

    internal IEnumerable<(string Dialect, DialectRenderer Renderer, string Expected)> Cases()
    {
        yield return ("SQLite", DialectRenderer.For<SqliteDialect>(), Sqlite);
        yield return ("PostgreSQL", DialectRenderer.For<PostgreSqlDialect>(), PostgreSql);
        yield return ("MySQL", DialectRenderer.For<MySqlDialect>(), MySql);
        yield return ("MariaDB", DialectRenderer.For<MariaDbDialect>(), MariaDb ?? MySql);
        yield return ("SQL Server", DialectRenderer.For<SqlServerDialect>(), SqlServer);
    }
}
