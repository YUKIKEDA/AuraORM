namespace AuraORM.Integration.Tests;

/// <summary>
/// A database that the integration tests run against: SQLite in process, or a server whose connection string comes from an
/// environment variable (<c>.cursor/rules/containers.mdc</c>).
/// </summary>
/// <typeparam name="TDialect">The dialect of the database.</typeparam>
public interface ITestTarget<TDialect> : IAsyncDisposable
    where TDialect : IDialect
{
    /// <summary>
    /// Gets a database whose <c>people</c> table exists and is empty. Each call gives a database of its own, so tests can run
    /// in parallel.
    /// </summary>
    /// <returns>The database.</returns>
    Task<Database<TDialect>> CreateDatabaseAsync();
}
