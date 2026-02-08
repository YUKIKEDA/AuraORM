using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

using Microsoft.Data.Sqlite;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class RawSqlQuerySQliteTests : IDisposable
{
    private readonly DatabaseFixture _fixture;

    public RawSqlQuerySQliteTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _fixture.SeedUsers();
        _fixture.SeedOrders();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    // ───────────────────────────────────────────────
    //  基本的な SELECT テスト（ToListAsync<T>）
    // ───────────────────────────────────────────────

    [Fact]
    public async Task パラメータなしの基本的な生SQLクエリを実行できる()
    {
        // Arrange
        var sqlText = "SELECT name FROM users;";

        // Act
        var query = _fixture.Db.RawSql(sqlText);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(6, actual.Count);
        Assert.Contains(actual, x => x.Name == "太郎");
        Assert.Contains(actual, x => x.Name == "花子");
        Assert.Contains(actual, x => x.Name == "次郎");
        Assert.Contains(actual, x => x.Name == "美咲");
        Assert.Contains(actual, x => x.Name == "健一");
        Assert.Contains(actual, x => x.Name == "さくら");
    }

    [Fact]
    public async Task 一つのパラメータを持つ生SQLクエリを実行できる()
    {
        // Arrange
        var sqlText = "SELECT name, age FROM users WHERE id = @id;";
        var parameters = new List<(string Name, object? Value)> { ("@id", 1) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Single(actual);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
    }

    [Fact]
    public async Task 複数のパラメータを持つ生SQLクエリを実行できる()
    {
        // Arrange
        var sqlText = "SELECT name, age FROM users WHERE name = @name AND age = @age;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("@name", "太郎"),
            ("@age", 25)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Single(actual);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
    }

    [Fact]
    public async Task NULLパラメータを含む生SQLクエリを実行できる()
    {
        // Arrange（@age IS NULL → TRUE になり全件取得される）
        var sqlText = "SELECT name, age FROM users WHERE @age IS NULL OR age > @age;";
        var parameters = new List<(string Name, object? Value)> { ("@age", null) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Equal(6, actual.Count);
    }

    [Fact]
    public async Task 複雑なJOINを含む生SQLクエリを実行できる()
    {
        // Arrange（users INNER JOIN orders, age > 18）
        // 次郎(18)は除外、さくらは注文なしで除外 → 6件
        var sqlText = @"
            SELECT u.name, o.productname
            FROM users u
            INNER JOIN orders o ON u.id = o.userid
            WHERE u.age > @minAge
            ORDER BY o.amount DESC;";
        var parameters = new List<(string Name, object? Value)> { ("@minAge", 18) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<NameAndProductNameResult>();

        // Assert
        Assert.Equal(6, actual.Count);
        Assert.Equal("健一", actual[0].UserName);     // 商品G: 4500
        Assert.Equal("商品G", actual[0].ProductName);
        Assert.Equal("美咲", actual[1].UserName);     // 商品E: 3200
        Assert.Equal("太郎", actual[2].UserName);     // 商品B: 2000
        Assert.Equal("花子", actual[3].UserName);     // 商品C: 1500
        Assert.Equal("太郎", actual[4].UserName);     // 商品A: 1000
        Assert.Equal("健一", actual[5].UserName);     // 商品F: 500
    }

    [Fact]
    public async Task サブクエリを含む生SQLクエリを実行できる()
    {
        // Arrange（amount > 1000 の注文を持つユーザー）
        // 商品B(2000), 商品C(1500), 商品E(3200), 商品G(4500) → 太郎, 花子, 美咲, 健一
        var sqlText = @"
            SELECT name FROM users
            WHERE id IN (
                SELECT userid FROM orders
                WHERE amount > @minTotal
            );";
        var parameters = new List<(string Name, object? Value)> { ("@minTotal", 1000) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(4, actual.Count);
        Assert.Contains(actual, x => x.Name == "太郎");
        Assert.Contains(actual, x => x.Name == "花子");
        Assert.Contains(actual, x => x.Name == "美咲");
        Assert.Contains(actual, x => x.Name == "健一");
    }

    [Fact]
    public async Task パラメータがnullの場合でも生SQLクエリを実行できる()
    {
        // Arrange
        var sqlText = "SELECT name FROM users;";

        // Act
        var query = _fixture.Db.RawSql(sqlText, null);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(6, actual.Count);
    }

    [Fact]
    public async Task 異なる型のパラメータを持つ生SQLクエリを実行できる()
    {
        // Arrange（string, int, int の3つの型）
        var sqlText = "SELECT name, age FROM users WHERE name = @name AND age >= @minAge AND age <= @maxAge;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("@name", "太郎"),
            ("@minAge", 20),
            ("@maxAge", 30)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Single(actual);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
    }

    // ───────────────────────────────────────────────
    //  DML テスト（ExecuteAsync）
    // ───────────────────────────────────────────────

    [Fact]
    public async Task UPDATE文を含む生SQLクエリを実行できる()
    {
        // Arrange
        var sqlText = "UPDATE users SET name = @name, age = @age WHERE id = @id;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("@name", "Jane"),
            ("@age", 30),
            ("@id", 1)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("Jane", row!.Value.Name);
        Assert.Equal(30, row.Value.Age);
    }

    [Fact]
    public async Task INSERT文を含む生SQLクエリを実行できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0);
        var sqlText = "INSERT INTO users (name, age, createdat) VALUES (@name, @age, @createdAt);";
        var parameters = new List<(string Name, object? Value)>
        {
            ("@name", "Bob"),
            ("@age", 35),
            ("@createdAt", createdAt.ToString("o"))
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Equal(7, CountUsers());
        var row = GetUserByName("Bob");
        Assert.NotNull(row);
        Assert.Equal(35, row!.Value.Age);
    }

    [Fact]
    public async Task DELETE文を含む生SQLクエリを実行できる()
    {
        // Arrange（さくら(id=6)は注文がないので外部キー制約なしで削除可能）
        var sqlText = "DELETE FROM users WHERE id = @id;";
        var parameters = new List<(string Name, object? Value)> { ("@id", 6) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Equal(5, CountUsers());
        Assert.Null(GetUserById(6));
    }

    // ───────────────────────────────────────────────
    //  高度な SQL テスト
    // ───────────────────────────────────────────────

    [Fact]
    public async Task ウィンドウ関数ROW_NUMBERを含む生SQLクエリを実行できる()
    {
        // Arrange（age DESC で上位3名を取得）
        // 健一(35), 花子(30), 美咲(28)
        var sqlText = @"
            SELECT name FROM (
                SELECT name, ROW_NUMBER() OVER (ORDER BY age DESC) as rn
                FROM users
            ) sub WHERE rn <= @limit;";
        var parameters = new List<(string Name, object? Value)> { ("@limit", 3) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(3, actual.Count);
        Assert.Equal("健一", actual[0].Name);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal("美咲", actual[2].Name);
    }

    [Fact]
    public async Task 複数のウィンドウ関数を含む生SQLクエリを実行できる()
    {
        // Arrange（ROW_NUMBER + LAG で上位3名を取得）
        var sqlText = @"
            SELECT name, age FROM (
                SELECT name, age,
                    ROW_NUMBER() OVER (ORDER BY age DESC) as rn,
                    LAG(age, 1) OVER (ORDER BY age ASC) as prev_age
                FROM users
            ) sub WHERE rn <= @limit;";
        var parameters = new List<(string Name, object? Value)> { ("@limit", 3) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Equal(3, actual.Count);
        Assert.Equal("健一", actual[0].Name);
        Assert.Equal(35, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal(30, actual[1].Age);
    }

    [Fact]
    public async Task CTEを含む生SQLクエリを実行できる()
    {
        // Arrange（注文合計 > 2000 のユーザーを取得）
        // 太郎(3000), 美咲(3200), 健一(5000)
        var sqlText = @"
            WITH high_spenders AS (
                SELECT userid, SUM(amount) as total
                FROM orders
                GROUP BY userid
                HAVING SUM(amount) > @minTotal
            )
            SELECT u.name FROM users u
            INNER JOIN high_spenders hs ON u.id = hs.userid
            ORDER BY u.name;";
        var parameters = new List<(string Name, object? Value)> { ("@minTotal", 2000) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(3, actual.Count);
        Assert.Contains(actual, x => x.Name == "太郎");
        Assert.Contains(actual, x => x.Name == "美咲");
        Assert.Contains(actual, x => x.Name == "健一");
    }

    [Fact]
    public async Task 複雑なCASE式と条件分岐を含む生SQLクエリを実行できる()
    {
        // Arrange（CASE で年齢カテゴリ分け → 'adult'（20〜29歳）を抽出）
        // 太郎(25), 美咲(28), さくら(22)
        var sqlText = @"
            SELECT name FROM users WHERE
                CASE
                    WHEN age < 20 THEN 'young'
                    WHEN age >= 20 AND age < 30 THEN 'adult'
                    ELSE 'senior'
                END = @category;";
        var parameters = new List<(string Name, object? Value)> { ("@category", "adult") };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(3, actual.Count);
        Assert.Contains(actual, x => x.Name == "太郎");
        Assert.Contains(actual, x => x.Name == "美咲");
        Assert.Contains(actual, x => x.Name == "さくら");
    }

    [Fact]
    public async Task EXISTS句と相関サブクエリを含む生SQLクエリを実行できる()
    {
        // Arrange（amount > 1000 の注文が存在するユーザー）
        // 太郎(商品B:2000), 花子(商品C:1500), 美咲(商品E:3200), 健一(商品G:4500)
        var sqlText = @"
            SELECT name FROM users u
            WHERE EXISTS (
                SELECT 1 FROM orders o
                WHERE o.userid = u.id
                AND o.amount > @minAmount
            );";
        var parameters = new List<(string Name, object? Value)> { ("@minAmount", 1000) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(4, actual.Count);
        Assert.Contains(actual, x => x.Name == "太郎");
        Assert.Contains(actual, x => x.Name == "花子");
        Assert.Contains(actual, x => x.Name == "美咲");
        Assert.Contains(actual, x => x.Name == "健一");
    }

    [Fact]
    public async Task UNION_ALLを含む生SQLクエリを実行できる()
    {
        // Arrange（20歳未満 UNION ALL 30歳超 → 次郎(18) + 健一(35) = 2件）
        var sqlText = @"
            SELECT name FROM users WHERE age < @youngAge
            UNION ALL
            SELECT name FROM users WHERE age > @oldAge;";
        var parameters = new List<(string Name, object? Value)>
        {
            ("@youngAge", 20),
            ("@oldAge", 30)
        };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(2, actual.Count);
        Assert.Contains(actual, x => x.Name == "次郎");
        Assert.Contains(actual, x => x.Name == "健一");
    }

    [Fact]
    public async Task GROUP_BYとHAVING句を含む生SQLクエリを実行できる()
    {
        // Arrange（注文数が2件以上のユーザー → 太郎(2件), 健一(2件)）
        var sqlText = @"
            SELECT u.name FROM users u
            INNER JOIN orders o ON u.id = o.userid
            GROUP BY u.id, u.name
            HAVING COUNT(o.id) > @minOrders;";
        var parameters = new List<(string Name, object? Value)> { ("@minOrders", 1) };

        // Act
        var query = _fixture.Db.RawSql(sqlText, parameters);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(2, actual.Count);
        Assert.Contains(actual, x => x.Name == "太郎");
        Assert.Contains(actual, x => x.Name == "健一");
    }

    // ───────────────────────────────────────────────
    //  ヘルパーメソッド
    // ───────────────────────────────────────────────

    private int CountUsers()
    {
        using var cmd = new SqliteCommand("SELECT COUNT(*) FROM users", _fixture.Connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private (string Name, int? Age)? GetUserById(int id)
    {
        using var cmd = new SqliteCommand(
            "SELECT name, age FROM users WHERE id = @id",
            _fixture.Connection);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;
        var name = reader.GetString(reader.GetOrdinal("name"));
        var age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("age"));
        return (name, age);
    }

    private (string Name, int? Age)? GetUserByName(string name)
    {
        using var cmd = new SqliteCommand(
            "SELECT name, age FROM users WHERE name = @name",
            _fixture.Connection);
        cmd.Parameters.AddWithValue("@name", name);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;
        var n = reader.GetString(reader.GetOrdinal("name"));
        var age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("age"));
        return (n, age);
    }
}
