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

        var createOrdersTable = @"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                userid INTEGER NOT NULL,
                productname TEXT NOT NULL,
                amount REAL NOT NULL,
                status INTEGER NOT NULL,
                orderdate TEXT NOT NULL,
                shippeddate TEXT,
                FOREIGN KEY (userid) REFERENCES users(id)
            )";

        using var cmd2 = new SqliteCommand(createOrdersTable, Connection);
        cmd2.ExecuteNonQuery();
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
            ("健一", 35, DateTime.Now),
            ("さくら", 22, DateTime.Now),
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
    /// テスト用の注文データを挿入する（SeedUsers の後に呼ぶこと）
    /// </summary>
    public void SeedOrders()
    {
        var baseDate = DateTime.Now;
        var orders = new[]
        {
            (UserId: 1, ProductName: "商品A", Amount: 1000m, Status: 0, OrderDate: baseDate, ShippedDate: (DateTime?)null),
            (UserId: 1, ProductName: "商品B", Amount: 2000m, Status: 2, OrderDate: baseDate, ShippedDate: (DateTime?)baseDate.AddDays(1)),
            (UserId: 2, ProductName: "商品C", Amount: 1500m, Status: 1, OrderDate: baseDate, ShippedDate: (DateTime?)null),
            (UserId: 3, ProductName: "商品D", Amount: 800m, Status: 0, OrderDate: baseDate, ShippedDate: (DateTime?)null),
            (UserId: 4, ProductName: "商品E", Amount: 3200m, Status: 3, OrderDate: baseDate, ShippedDate: (DateTime?)baseDate.AddDays(2)),
            (UserId: 5, ProductName: "商品F", Amount: 500m, Status: 0, OrderDate: baseDate, ShippedDate: (DateTime?)null),
            (UserId: 5, ProductName: "商品G", Amount: 4500m, Status: 2, OrderDate: baseDate, ShippedDate: (DateTime?)baseDate.AddDays(1)),
        };

        foreach (var (userId, productName, amount, status, orderDate, shippedDate) in orders)
        {
            using var cmd = new SqliteCommand(
                "INSERT INTO orders (userid, productname, amount, status, orderdate, shippeddate) VALUES (@userId, @productName, @amount, @status, @orderDate, @shippedDate)",
                Connection);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@productName", productName);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@orderDate", orderDate.ToString("o"));
            cmd.Parameters.AddWithValue("@shippedDate", shippedDate?.ToString("o") ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// テストデータをリセットする
    /// </summary>
    public void ResetDatabase()
    {
        using var cmd1 = new SqliteCommand("DELETE FROM orders", Connection);
        cmd1.ExecuteNonQuery();
        using var cmd2 = new SqliteCommand("DELETE FROM users", Connection);
        cmd2.ExecuteNonQuery();

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
