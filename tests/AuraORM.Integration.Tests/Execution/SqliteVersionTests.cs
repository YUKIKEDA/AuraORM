namespace AuraORM.Integration.Tests;

/// <summary>
/// The version of SQLite that the SQLite tests ran on (design §1.3, §17).
/// </summary>
public sealed class SqliteVersionTests
{
    /// <summary>
    /// A build with <c>-p:AuraSqliteMinimum=true</c> runs SQLite 3.39, the minimum version; the default build runs the
    /// version that Microsoft.Data.Sqlite bundles, which is not older (design §1.3, §17).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An in-memory SQLite database.
    ///
    /// Steps:
    /// - Read <c>sqlite_version()</c>.
    ///
    /// Expected:
    /// - With <c>AURA_SQLITE_MINIMUM</c>: a version that starts with <c>3.39.</c>. Without: version 3.39 or later.
    /// </remarks>
    [Fact]
    public async Task Version_IsTheOneOfTheBuild()
    {
        await using var target = new SqliteTarget();
        Database<SqliteDialect> db = await target.CreateDatabaseAsync();

        string version = await db.QueryScalarAsync<string>($"SELECT sqlite_version()", TestContext.Current.CancellationToken);

#if AURA_SQLITE_MINIMUM
        Assert.StartsWith("3.39.", version, StringComparison.Ordinal);
#else
        Assert.True(Version.Parse(version) >= new Version(3, 39), version);
#endif
    }
}
