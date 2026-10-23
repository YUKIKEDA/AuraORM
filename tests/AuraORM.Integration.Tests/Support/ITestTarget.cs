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
    /// Gets a database whose <c>people</c>, <c>typed_values</c>, and <c>pets</c> tables exist and are empty. On SQLite each call gives a new
    /// database; on a server each call drops and creates the tables again, which empties the databases of earlier calls, so
    /// the tests of one server run one at a time (an xUnit collection).
    /// </summary>
    /// <returns>The database.</returns>
    Task<Database<TDialect>> CreateDatabaseAsync();
}
