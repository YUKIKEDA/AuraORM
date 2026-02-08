using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

using Microsoft.Data.Sqlite;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class DeleteQuerySQliteTests : IDisposable
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly DatabaseFixture _fixture;

    public DeleteQuerySQliteTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _fixture.SeedUsers();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task Deleteで何もしていない場合は指定したテーブルの全データを削除する()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Delete(UserTable);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        Assert.Equal(0, CountUsers());
    }

    [Fact]
    public async Task Where句で1つの条件を指定してDELETE句に追加できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Id == 1);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Null(GetUserById(1));
        Assert.Equal(5, CountUsers());
    }

    [Fact]
    public async Task Where句で複数の条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age > 20 かつ Name != "Bob" → 太郎, 花子, 美咲, 健一, さくら の5件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        Assert.Equal(1, CountUsers());
        Assert.NotNull(GetUserByName("次郎")); // 次郎(18)のみ残る
    }

    [Fact]
    public async Task Where句で複数の条件をチェーンしてDELETE句に追加できる()
    {
        // Arrange（Age > 20 かつ Name != "Bob" → 5件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age > 20).Where(UserTable.Name != "Bob");
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        Assert.Equal(1, CountUsers());
    }

    [Fact]
    public async Task Where句でIN条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age IN (20, 30, 40) → 花子(30) の1件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age.In(20, 30, 40));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Null(GetUserByName("花子"));
        Assert.Equal(5, CountUsers());
    }

    [Fact]
    public async Task Where句でNOT_IN条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age NOT IN (20, 30, 40) → 5件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age.NotIn(20, 30, 40));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        Assert.Equal(1, CountUsers());
        Assert.NotNull(GetUserByName("花子")); // 花子(30)のみ残る
    }

    [Fact]
    public async Task Where句でIS_NULL条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Name IS NULL → シードに該当なしで0件）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Name.IsNull());
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(0, affected);
        Assert.Equal(6, CountUsers());
    }

    [Fact]
    public async Task Where句でIS_NOT_NULL条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Name IS NOT NULL → 全6件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Name.IsNotNull());
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        Assert.Equal(0, CountUsers());
    }

    [Fact]
    public async Task Where句でBETWEEN条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age BETWEEN 20 AND 40 → 次郎(18)以外の5件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age.Between(20, 40));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        Assert.Equal(1, CountUsers());
        Assert.NotNull(GetUserByName("次郎"));
    }

    [Fact]
    public async Task Where句でOR条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Name == "Alice" OR Age > 20 → Aliceはいないので Age > 20 の5件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where((UserTable.Name == "Alice").Or(UserTable.Age > 20));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        Assert.Equal(1, CountUsers());
    }

    [Fact]
    public async Task Where句でLIKE条件を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Name LIKE '太%' → 太郎の1件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Name.Like("太%"));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Null(GetUserByName("太郎"));
        Assert.Equal(5, CountUsers());
    }

    [Fact]
    public async Task Where句でより小さい演算子を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age < 20 → 次郎(18) の1件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age < 20);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Null(GetUserByName("次郎"));
        Assert.Equal(5, CountUsers());
    }

    [Fact]
    public async Task Where句でより大きいか等しい演算子を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age >= 20 → 次郎(18)以外の5件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age >= 20);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        Assert.Equal(1, CountUsers());
        Assert.NotNull(GetUserByName("次郎"));
    }

    [Fact]
    public async Task Where句でより小さいか等しい演算子を組み合わせてDELETE句に追加できる()
    {
        // Arrange（Age <= 20 → 次郎(18) の1件削除）
        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age <= 20);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        Assert.Null(GetUserByName("次郎"));
        Assert.Equal(5, CountUsers());
    }

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
