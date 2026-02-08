using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

using Microsoft.Data.Sqlite;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class InsertQuerySQliteTests : IDisposable
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly DatabaseFixture _fixture;

    public InsertQuerySQliteTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public void Valuesで指定した値をINSERT句に追加できる()
    {
        // Arrange（createdat は NOT NULL のため必須。Age のみ省略して挿入）
        var createdAt = new DateTime(2024, 1, 15, 12, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "John"), (UserTable.Age, 20), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserByName("John");
        Assert.NotNull(row);
        Assert.Equal("John", row!.Name);
        Assert.Equal(20, row.Age);
        Assert.Equal(createdAt.ToString("o"), row.CreatedAt.ToString("o"));
    }

    [Fact]
    public void Valuesで1つのカラムのみを指定してINSERT句に追加できる()
    {
        // Arrange（name と createdat は NOT NULL のため両方指定）
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Alice"), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserByName("Alice");
        Assert.NotNull(row);
        Assert.Equal("Alice", row!.Name);
        Assert.Null(row.Age);
    }

    [Fact]
    public void Valuesで3つ以上のカラムを指定してINSERT句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Bob"), (UserTable.Age, 30), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserByName("Bob");
        Assert.NotNull(row);
        Assert.Equal("Bob", row!.Name);
        Assert.Equal(30, row.Age);
        Assert.Equal(createdAt.ToString("o"), row.CreatedAt.ToString("o"));
    }

    [Fact]
    public void ValuesでNULL値を指定してINSERT句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 2, 1, 0, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Charlie"), (UserTable.Age, null), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserByName("Charlie");
        Assert.NotNull(row);
        Assert.Equal("Charlie", row!.Name);
        Assert.Null(row.Age);
    }

    [Fact]
    public void ValuesですべてのINSERT可能なカラムを指定してINSERT句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "David"), (UserTable.Age, 25), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(1, affected);
        var row = GetUserByName("David");
        Assert.NotNull(row);
        Assert.Equal("David", row!.Name);
        Assert.Equal(25, row.Age);
        Assert.Equal(createdAt.ToString("o"), row.CreatedAt.ToString("o"));
    }

    [Fact]
    public void Valuesで空のオブジェクトを指定した場合は実行時にエラーになる()
    {
        // TODO コンパイル時に検証できないか要検討
        // Arrange（空の VALUES は NOT NULL 制約で失敗する）
        var query = _fixture.Db.Insert(UserTable).Values([]);

        // Act & Assert
        var ex = Assert.Throws<SqliteException>(() => query.Execute());
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Valuesで複数行を指定してINSERT句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "John"), (UserTable.Age, 20), (UserTable.CreatedAt, createdAt) },
            { (UserTable.Name, "Jane"), (UserTable.Age, 25), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(2, affected);
        var john = GetUserByName("John");
        var jane = GetUserByName("Jane");
        Assert.NotNull(john);
        Assert.NotNull(jane);
        Assert.Equal(20, john.Age);
        Assert.Equal(25, jane.Age);
    }

    [Fact]
    public void Valuesで3行以上を指定してINSERT句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Alice"), (UserTable.Age, 20), (UserTable.CreatedAt, createdAt) },
            { (UserTable.Name, "Bob"), (UserTable.Age, 25), (UserTable.CreatedAt, createdAt) },
            { (UserTable.Name, "Charlie"), (UserTable.Age, 30), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(3, affected);
        Assert.NotNull(GetUserByName("Alice"));
        Assert.NotNull(GetUserByName("Bob"));
        Assert.NotNull(GetUserByName("Charlie"));
    }

    [Fact]
    public void Valuesで複数行を指定しNULL値を含むINSERT句に追加できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "David"), (UserTable.Age, null), (UserTable.CreatedAt, createdAt) },
            { (UserTable.Name, "Eve"), (UserTable.Age, 28), (UserTable.CreatedAt, createdAt) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(2, affected);
        var david = GetUserByName("David");
        var eve = GetUserByName("Eve");
        Assert.NotNull(david);
        Assert.Null(david.Age);
        Assert.NotNull(eve);
        Assert.Equal(28, eve.Age);
    }

    [Fact]
    public void Valuesで複数行を指定し3つ以上のカラムを含むINSERT句に追加できる()
    {
        // Arrange
        var createdAt1 = new DateTime(2024, 1, 1, 12, 0, 0);
        var createdAt2 = new DateTime(2024, 1, 2, 12, 0, 0);

        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Frank"), (UserTable.Age, 35), (UserTable.CreatedAt, createdAt1) },
            { (UserTable.Name, "Grace"), (UserTable.Age, 40), (UserTable.CreatedAt, createdAt2) }
        });
        var affected = query.Execute();

        // Assert
        Assert.Equal(2, affected);
        var frank = GetUserByName("Frank");
        var grace = GetUserByName("Grace");
        Assert.NotNull(frank);
        Assert.Equal(35, frank.Age);
        Assert.Equal(createdAt1.ToString("o"), frank.CreatedAt.ToString("o"));
        Assert.NotNull(grace);
        Assert.Equal(40, grace.Age);
        Assert.Equal(createdAt2.ToString("o"), grace.CreatedAt.ToString("o"));
    }

    private sealed record UserRow(string Name, int? Age, DateTime CreatedAt);

    private UserRow? GetUserByName(string name)
    {
        using var cmd = new SqliteCommand(
            "SELECT name, age, createdat FROM users WHERE name = @name",
            _fixture.Connection);
        cmd.Parameters.AddWithValue("@name", name);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;
        var age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("age"));
        var createdAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("createdat")));
        return new UserRow(name, age, createdAt);
    }
}
