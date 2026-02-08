using AuraORM.Builder.Query;
using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Query;

public class InsertQueryTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly TestFixture _fixture = new();

    [Fact]
    public void Valuesで指定した値をINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "John"), (UserTable.Age, 20) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\") VALUES (@p0, @p1);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }

    [Fact]
    public void Valuesで1つのカラムのみを指定してINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Alice") }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\") VALUES (@p0);", sqlText);
        Assert.Equal([("@p0", "Alice")], parameters);
    }

    [Fact]
    public void Valuesで3つ以上のカラムを指定してINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Bob"), (UserTable.Age, 30), (UserTable.CreatedAt, createdAt) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\", \"createdat\") VALUES (@p0, @p1, @p2);", sqlText);
        Assert.Equal([("@p0", "Bob"), ("@p1", 30), ("@p2", createdAt)], parameters);
    }

    [Fact]
    public void ValuesでNULL値を指定してINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Charlie"), (UserTable.Age, null) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\") VALUES (@p0, @p1);", sqlText);
        Assert.Equal([("@p0", "Charlie"), ("@p1", null)], parameters);
    }

    [Fact]
    public void ValuesですべてのINSERT可能なカラムを指定してINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "David"), (UserTable.Age, 25), (UserTable.CreatedAt, createdAt) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\", \"createdat\") VALUES (@p0, @p1, @p2);", sqlText);
        Assert.Equal([("@p0", "David"), ("@p1", 25), ("@p2", createdAt)], parameters);
    }

    [Fact]
    public void Valuesで空のオブジェクトを指定した場合は空のINSERT句になるSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values([]);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" () VALUES ();", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Valuesで複数行を指定してINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "John"), (UserTable.Age, 20) },
            { (UserTable.Name, "Jane"), (UserTable.Age, 25) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\") VALUES (@p0, @p1), (@p2, @p3);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20), ("@p2", "Jane"), ("@p3", 25)], parameters);
    }

    [Fact]
    public void Valuesで3行以上を指定してINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "Alice"), (UserTable.Age, 20) },
            { (UserTable.Name, "Bob"), (UserTable.Age, 25) },
            { (UserTable.Name, "Charlie"), (UserTable.Age, 30) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\") VALUES (@p0, @p1), (@p2, @p3), (@p4, @p5);", sqlText);
        Assert.Equal([
            ("@p0", "Alice"), ("@p1", 20),
            ("@p2", "Bob"), ("@p3", 25),
            ("@p4", "Charlie"), ("@p5", 30)
        ], parameters);
    }

    [Fact]
    public void Valuesで複数行を指定しNULL値を含むINSERT句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Insert(UserTable).Values(new()
        {
            { (UserTable.Name, "David"), (UserTable.Age, null) },
            { (UserTable.Name, "Eve"), (UserTable.Age, 28) }
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\") VALUES (@p0, @p1), (@p2, @p3);", sqlText);
        Assert.Equal([("@p0", "David"), ("@p1", null), ("@p2", "Eve"), ("@p3", 28)], parameters);
    }

    [Fact]
    public void Valuesで複数行を指定し3つ以上のカラムを含むINSERT句に追加するSQLが生成できる()
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
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("INSERT INTO \"users\" (\"name\", \"age\", \"createdat\") VALUES (@p0, @p1, @p2), (@p3, @p4, @p5);", sqlText);
        Assert.Equal([
            ("@p0", "Frank"), ("@p1", 35), ("@p2", createdAt1),
            ("@p3", "Grace"), ("@p4", 40), ("@p5", createdAt2)
        ], parameters);
    }
}
