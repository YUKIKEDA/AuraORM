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

    public const string CreateTypedValues = """
        CREATE TABLE typed_values (
          id INTEGER PRIMARY KEY,
          flag_value INTEGER,
          tiny_value INTEGER,
          small_value INTEGER,
          number_value INTEGER,
          big_value INTEGER,
          single_value REAL,
          double_value REAL,
          money_value TEXT,
          text_value TEXT,
          blob_value BLOB,
          key_value TEXT,
          at_value TEXT,
          at_offset_value TEXT,
          day_value TEXT,
          time_value TEXT,
          state_value INTEGER
        )
        """;

    public const string CreatePets = """
        CREATE TABLE pets (
          id INTEGER PRIMARY KEY,
          owner_id INTEGER,
          name TEXT NOT NULL
        )
        """;

    public const string CreateNotes = "CREATE TABLE notes (id INTEGER PRIMARY KEY AUTOINCREMENT, body TEXT, priority INTEGER NOT NULL DEFAULT 0)";

    public const string CreateOrders = "CREATE TABLE orders (id INTEGER PRIMARY KEY, parent_id INTEGER, link TEXT, total TEXT)";

    private readonly List<SqliteConnection> _connections = [];

    public async Task<Database<SqliteDialect>> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        _connections.Add(connection);
        await connection.OpenAsync();
        foreach (string ddl in (string[])[CreatePeople, CreateTypedValues, CreatePets, CreateNotes, CreateOrders])
        {
            await using SqliteCommand create = connection.CreateCommand();
            create.CommandText = ddl;
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
