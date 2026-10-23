using Microsoft.Data.Sqlite;

namespace AuraORM.Integration.Tests;

/// <summary>
/// SQLite in memory: every database is a new connection to a new in-memory database.
/// </summary>
public sealed class SqliteTarget : ITestTarget<SqliteDialect>
{
    public const string CreatePeople = """
        CREATE TABLE people (
          id INTEGER PRIMARY KEY,
          name TEXT NOT NULL,
          nickname TEXT,
          age INTEGER,
          status INTEGER NOT NULL,
          balance TEXT NOT NULL,
          birthday TEXT NOT NULL
        )
        """;

    private readonly List<SqliteConnection> _connections = [];

    public async Task<Database<SqliteDialect>> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        _connections.Add(connection);
        await connection.OpenAsync();
        await using (SqliteCommand create = connection.CreateCommand())
        {
            create.CommandText = CreatePeople;
            await create.ExecuteNonQueryAsync();
        }

        return new Database<SqliteDialect>(connection);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (SqliteConnection connection in _connections)
        {
            await connection.DisposeAsync();
        }
    }
}
