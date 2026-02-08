using AuraORM.Builder.Query;
using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Query;

public class UpdateQueryTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly TestFixture _fixture = new();

    [Fact]
    public void Setで指定した値をUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John"), (UserTable.Age, 20)
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0, \"age\" = @p1;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }

    [Fact]
    public void Setで1つのカラムのみを指定してUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Alice")
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0;", sqlText);
        Assert.Equal([("@p0", "Alice")], parameters);
    }

    [Fact]
    public void Setで3つ以上のカラムを指定してUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Bob"), (UserTable.Age, 30), (UserTable.CreatedAt, createdAt)
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0, \"age\" = @p1, \"createdat\" = @p2;", sqlText);
        Assert.Equal([("@p0", "Bob"), ("@p1", 30), ("@p2", createdAt)], parameters);
    }

    [Fact]
    public void SetでNULL値を指定してUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Charlie"), (UserTable.Age, null)
        });
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0, \"age\" = @p1;", sqlText);
        Assert.Equal([("@p0", "Charlie"), ("@p1", null)], parameters);
    }

    [Fact]
    public void SetとWhere句を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John") , (UserTable.Age, 20)
        }).Where(UserTable.Id == 1);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0, \"age\" = @p1 WHERE \"id\" = @p2;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20), ("@p2", 1)], parameters);
    }

    [Fact]
    public void Setと複数のWhere条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "Alice")
        }).Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE (\"age\" > @p1 AND \"name\" <> @p2);", sqlText);
        Assert.Equal([("@p0", "Alice"), ("@p1", 20), ("@p2", "Bob")], parameters);
    }

    [Fact]
    public void Setと複数のWhere条件をチェーンしてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age > 20).Where(UserTable.Name != "Bob");
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" > @p1 AND \"name\" <> @p2;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20), ("@p2", "Bob")], parameters);
    }

    [Fact]
    public void SetとIN条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age.In(20, 30, 40));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" IN (@p1, @p2, @p3);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20), ("@p2", 30), ("@p3", 40)], parameters);
    }

    [Fact]
    public void SetとNOT_IN条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age.NotIn(20, 30, 40));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" NOT IN (@p1, @p2, @p3);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20), ("@p2", 30), ("@p3", 40)], parameters);
    }

    [Fact]
    public void SetとIS_NULL条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Name.IsNull());
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"name\" IS NULL;", sqlText);
        Assert.Equal([("@p0", "John")], parameters);
    }

    [Fact]
    public void SetとIS_NOT_NULL条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Name.IsNotNull());
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"name\" IS NOT NULL;", sqlText);
        Assert.Equal([("@p0", "John")], parameters);
    }

    [Fact]
    public void SetとBETWEEN条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age.Between(20, 40));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" BETWEEN @p1 AND @p2;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20), ("@p2", 40)], parameters);
    }

    [Fact]
    public void SetとOR条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where((UserTable.Name == "Alice").Or(UserTable.Age > 20));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE (\"name\" = @p1 OR \"age\" > @p2);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", "Alice"), ("@p2", 20)], parameters);
    }

    [Fact]
    public void SetとLIKE条件を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Name.Like("John%"));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"name\" LIKE @p1;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", "John%")], parameters);
    }

    [Fact]
    public void Setとより小さい演算子を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age < 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" < @p1;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }

    [Fact]
    public void Setとより大きいか等しい演算子を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age >= 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" >= @p1;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }

    [Fact]
    public void Setとより小さいか等しい演算子を組み合わせてUPDATE句に追加するSQLが生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Update(UserTable).Set(new()
        {
            (UserTable.Name, "John")
        }).Where(UserTable.Age <= 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("UPDATE \"users\" SET \"name\" = @p0 WHERE \"age\" <= @p1;", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }
}
