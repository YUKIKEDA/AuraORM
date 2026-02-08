using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

using Microsoft.Data.Sqlite;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class UpdateQuerySQliteTests : IDisposable
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly DatabaseFixture _fixture;

    public UpdateQuerySQliteTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _fixture.SeedUsers();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task Setで指定した値をUPDATE句に追加できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John"), (UserTable.Age, 20)
        });
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
        Assert.Equal(20, row.Age);
    }

    [Fact]
    public async Task Setで1つのカラムのみを指定してUPDATE句に追加できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Alice")
        });
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("Alice", row!.Name);
    }

    [Fact]
    public async Task Setで3つ以上のカラムを指定してUPDATE句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Bob"), (UserTable.Age, 30), (UserTable.CreatedAt, createdAt)
        });
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("Bob", row!.Name);
        Assert.Equal(30, row.Age);
        Assert.Equal(createdAt.ToString("o"), row.CreatedAt.ToString("o"));
    }

    [Fact]
    public async Task SetでNULL値を指定してUPDATE句に追加できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Charlie"), (UserTable.Age, null)
        });
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("Charlie", row!.Name);
        Assert.Null(row.Age);
    }

    [Fact]
    public async Task SetとWhere句を組み合わせてUPDATE句に追加できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John"), (UserTable.Age, 20)
        }).Where(UserTable.Id == 1);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        var updated = GetUserById(1);
        Assert.NotNull(updated);
        Assert.Equal("John", updated!.Name);
        Assert.Equal(20, updated.Age);
        var unchanged = GetUserById(2);
        Assert.NotNull(unchanged);
        Assert.Equal("花子", unchanged!.Name);
        Assert.Equal(30, unchanged.Age);
    }

    [Fact]
    public async Task Setと複数のWhere条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age > 20 かつ Name != "Bob" → 太郎, 花子, 美咲, 健一, さくら の5件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Alice")
        }).Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("Alice", row!.Name);
    }

    [Fact]
    public async Task Setと複数のWhere条件をチェーンしてUPDATE句に追加できる()
    {
        // Arrange（Age > 20 かつ Name != "Bob" → 5件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age > 20).Where(UserTable.Name != "Bob");
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task SetとIN条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age IN (20, 30, 40) → 花子(30) の1件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age.In(20, 30, 40));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserById(2);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task SetとNOT_IN条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age NOT IN (20, 30, 40) → 25, 18, 28, 35, 22 の5件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age.NotIn(20, 30, 40));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task SetとIS_NULL条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Name IS NULL → シードデータに該当なしで0件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Name.IsNull());
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(0, affected);
    }

    [Fact]
    public async Task SetとIS_NOT_NULL条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Name IS NOT NULL → 全6件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Name.IsNotNull());
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(6, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task SetとBETWEEN条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age BETWEEN 20 AND 40 → 次郎(18)以外の5件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age.Between(20, 40));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
        var unchanged = GetUserById(3);
        Assert.NotNull(unchanged);
        Assert.Equal("次郎", unchanged!.Name);
    }

    [Fact]
    public async Task SetとOR条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Name == "Alice" OR Age > 20 → Aliceはいないので Age > 20 の5件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where((UserTable.Name == "Alice").Or(UserTable.Age > 20));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task SetとLIKE条件を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Name LIKE '太%' → 太郎の1件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Name.Like("太%"));
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task Setとより小さい演算子を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age < 20 → 次郎(18) の1件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age < 20);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserById(3);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task Setとより大きいか等しい演算子を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age >= 20 → 次郎(18)以外の5件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age >= 20);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(5, affected);
        var row = GetUserById(1);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    [Fact]
    public async Task Setとより小さいか等しい演算子を組み合わせてUPDATE句に追加できる()
    {
        // Arrange（Age <= 20 → 次郎(18) の1件）
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age <= 20);
        var affected = await query.ExecuteAsync();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserById(3);
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
    }

    private sealed record UserRow(string Name, int? Age, DateTime CreatedAt);

    private UserRow? GetUserById(int id)
    {
        using var cmd = new SqliteCommand(
            "SELECT name, age, createdat FROM users WHERE id = @id",
            _fixture.Connection);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;
        var name = reader.GetString(reader.GetOrdinal("name"));
        var age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("age"));
        var createdAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("createdat")));
        return new UserRow(name, age, createdAt);
    }
}
