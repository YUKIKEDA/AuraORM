using Microsoft.Data.Sqlite;
using AuraORM.Builder;

namespace AuraORM.Tests.Fixtures;

/// <summary>
/// テスト用のフィクスチャクラス
/// </summary>
public class DatabaseFixture : IDisposable
{
    /// <summary>
    /// データベース接続
    /// </summary>
    public SqliteConnection Connection { get; }

    /// <summary>
    /// データベースコンテキスト
    /// </summary>
    public DbContext Db { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public DatabaseFixture()
    {
        // インメモリデータベースを使用
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();

        InitializeDatabase();

        var schema = new Schema();

        Db = new DbContext(Connection, schema);
    }

    private void InitializeDatabase()
    {
        var createUsersTable = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER,
                createdat TEXT NOT NULL
            )";

        using var cmd1 = new SqliteCommand(createUsersTable, Connection);
        cmd1.ExecuteNonQuery();
    }

    /// <summary>
    /// テスト用のユーザーデータを挿入する
    /// </summary>
    public void SeedUsers()
    {
        var users = new[]
        {
            ("太郎", 25, DateTime.Now),
            ("花子", 30, DateTime.Now),
            ("次郎", 18, DateTime.Now),
            ("美咲", 28, DateTime.Now),
        };

        foreach (var (name, age, createdAt) in users)
        {
            using var cmd = new SqliteCommand(
                "INSERT INTO users (name, age, createdat) VALUES (@name, @age, @createdAt)",
                Connection);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@age", age);
            cmd.Parameters.AddWithValue("@createdAt", createdAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// テストデータをリセットする
    /// </summary>
    public void ResetDatabase()
    {
        using var cmd1 = new SqliteCommand("DELETE FROM users", Connection);
        cmd1.ExecuteNonQuery();

        // AutoIncrementをリセット
        using var cmd3 = new SqliteCommand("DELETE FROM sqlite_sequence", Connection);
        cmd3.ExecuteNonQuery();
    }

    public void Dispose()
    {
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
