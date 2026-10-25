using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace AuraORM.Benchmarks;

/// <summary>
/// Runs the SQL that each library renders on SQLite in memory, so that <c>--check</c> shows that the compared statements
/// return the same rows, though their text differs (aliases, quotes, line breaks).
/// </summary>
internal static partial class Statements
{
    public static SqliteConnection Open()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE people (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Age INTEGER NOT NULL, Balance NUMERIC NOT NULL, CreatedAt TEXT NOT NULL, Active INTEGER NOT NULL);
            CREATE TABLE pets (Id INTEGER PRIMARY KEY, OwnerId INTEGER NOT NULL, Name TEXT NOT NULL);
            CREATE TABLE toys (Id INTEGER PRIMARY KEY, PetId INTEGER NOT NULL, Price NUMERIC NOT NULL);
            INSERT INTO people VALUES (1, 'Alice', 25, 1.5, '2024-01-01', 1), (2, 'Bob', 31, 2.5, '2024-01-02', 0), (3, 'Bob', 40, 3.5, '2024-01-03', 1);
            INSERT INTO pets VALUES (1, 1, 'Rex'), (2, 2, 'Tom'), (3, 3, 'Kit'), (4, 3, 'Max');
            INSERT INTO toys VALUES (1, 1, 1.0), (2, 2, 2.0), (3, 3, 3.0), (4, 4, 4.0), (5, 4, 5.0);
            """;
        _ = command.ExecuteNonQuery();
        return connection;
    }

    /// <summary>
    /// Runs both statements, binding each parameter that the SQL names to 30 (the value of the compared statements), and
    /// prints whether they return the same rows. The order of the rows and of the columns is not compared: EF Core puts
    /// the key first and the other columns in the order of their names.
    /// </summary>
    public static int Same(SqliteConnection connection, string name, string expected, string actual)
    {
        bool same = Rows(connection, expected).SequenceEqual(Rows(connection, actual));
        Console.WriteLine((same ? "ok   " : "FAIL ") + name + (same ? string.Empty : ": " + actual));
        return same ? 0 : 1;
    }

    private static List<string> Rows(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (string parameter in Parameter().Matches(sql).Select(m => m.Value).Distinct())
        {
            _ = command.Parameters.AddWithValue(parameter, 30);
        }

        var rows = new List<string>();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            _ = reader.GetValues(values);
            rows.Add(string.Join("|", values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)).Order(StringComparer.Ordinal)));
        }

        rows.Sort(StringComparer.Ordinal);
        return rows;
    }

    [GeneratedRegex(@"@\w+")]
    private static partial Regex Parameter();
}
