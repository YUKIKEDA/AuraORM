using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Query;

public class DeleteQueryTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly TestFixture _fixture = new();

    [Fact]
    public void Deleteで何もしていない場合は指定したテーブルの全データを削除するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable);
        var (sqlText, parameters) = query.Build();

        // Assert
        // SQLテキストが DELETE FROM "users"; になる
        Assert.Equal("DELETE FROM \"users\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Where句で1つの条件を指定してDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Id == 1);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"id\" = @p0;", sqlText);
        Assert.Equal([("@p0", 1)], parameters);
    }

    [Fact]
    public void Where句で複数の条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE (\"age\" > @p0 AND \"name\" <> @p1);", sqlText);
        Assert.Equal([("@p0", 20), ("@p1", "Bob")], parameters);
    }

    [Fact]
    public void Where句で複数の条件をチェーンしてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age > 20).Where(UserTable.Name != "Bob");
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" > @p0 AND \"name\" <> @p1;", sqlText);
        Assert.Equal([("@p0", 20), ("@p1", "Bob")], parameters);
    }

    [Fact]
    public void Where句でIN条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age.In(20, 30, 40));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" IN (@p0, @p1, @p2);", sqlText);
        Assert.Equal([("@p0", 20), ("@p1", 30), ("@p2", 40)], parameters);
    }

    [Fact]
    public void Where句でNOT_IN条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age.NotIn(20, 30, 40));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" NOT IN (@p0, @p1, @p2);", sqlText);
        Assert.Equal([("@p0", 20), ("@p1", 30), ("@p2", 40)], parameters);
    }

    [Fact]
    public void Where句でIS_NULL条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Name.IsNull());
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"name\" IS NULL;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Where句でIS_NOT_NULL条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Name.IsNotNull());
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"name\" IS NOT NULL;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Where句でBETWEEN条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age.Between(20, 40));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" BETWEEN @p0 AND @p1;", sqlText);
        Assert.Equal([("@p0", 20), ("@p1", 40)], parameters);
    }

    [Fact]
    public void Where句でOR条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where((UserTable.Name == "Alice").Or(UserTable.Age > 20));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE (\"name\" = @p0 OR \"age\" > @p1);", sqlText);
        Assert.Equal([("@p0", "Alice"), ("@p1", 20)], parameters);
    }

    [Fact]
    public void Where句でLIKE条件を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Name.Like("John%"));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"name\" LIKE @p0;", sqlText);
        Assert.Equal([("@p0", "John%")], parameters);
    }

    [Fact]
    public void Where句でより小さい演算子を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age < 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" < @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void Where句でより大きいか等しい演算子を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age >= 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" >= @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void Where句でより小さいか等しい演算子を組み合わせてDELETE句に追加するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Delete(UserTable).Where(UserTable.Age <= 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("DELETE FROM \"users\" WHERE \"age\" <= @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }
}
