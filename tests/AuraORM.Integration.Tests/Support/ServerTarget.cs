using System.Data.Common;

namespace AuraORM.Integration.Tests;

/// <summary>
/// A database server whose connection string comes from an environment variable (<c>tools/databases.json</c>,
/// <c>.cursor/rules/containers.mdc</c>). Each test gets an empty <c>people</c> table; the test classes of one server share an
/// xUnit collection, so they run one at a time. The same holds for <c>typed_values</c>. The table lives in a database of this test process (<see cref="DatabaseName"/>),
/// so the <c>net8.0</c> and <c>net10.0</c> test processes, which <c>dotnet test</c> runs at the same time, do not share it.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class ServerTarget<TDialect> : ITestTarget<TDialect>
    where TDialect : IDialect
{
    private DbDataSource? _dataSource;

    /// <summary>
    /// Gets the name of the database in <c>tools/databases.json</c>, which also names the environment variable
    /// <c>AURA_TEST_{NAME}</c>.
    /// </summary>
    protected abstract string Name { get; }

    /// <summary>
    /// Gets the statements that drop and create the <c>people</c> and <c>typed_values</c> tables, in the server's own DDL.
    /// </summary>
    protected abstract IReadOnlyList<string> ResetTables { get; }

    /// <summary>
    /// Gets the name of the database that this test process uses: <c>aura_net8</c> under .NET 8, <c>aura_net10</c> under .NET 10.
    /// </summary>
    protected static string DatabaseName { get; } = "aura_net" + Environment.Version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<Database<TDialect>> CreateDatabaseAsync()
    {
        _dataSource ??= await CreateDataSourceAsync(ConnectionString());
        await using DbConnection connection = await _dataSource.OpenConnectionAsync();
        foreach (string statement in ResetTables)
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }

        return new Database<TDialect>(_dataSource);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates the data source on <see cref="DatabaseName"/>, after creating that database if it does not exist.
    /// </summary>
    /// <param name="connectionString">The connection string from the environment.</param>
    /// <returns>The data source.</returns>
    protected abstract Task<DbDataSource> CreateDataSourceAsync(string connectionString);

    private string ConnectionString()
    {
        string variable = "AURA_TEST_" + Name.ToUpperInvariant();
        string? connectionString = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // In CI the job of this database names it in AURA_TEST_REQUIRE: a skip there would hide a broken setup.
        string required = Environment.GetEnvironmentVariable("AURA_TEST_REQUIRE") ?? string.Empty;
        if (required.Split(',').Contains(Name, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{variable} is not set, but AURA_TEST_REQUIRE names {Name}.");
        }

        Assert.Skip($"{Name}: {variable} is not set (tools/db.ps1 up starts the database locally).");
        return string.Empty;
    }
}
