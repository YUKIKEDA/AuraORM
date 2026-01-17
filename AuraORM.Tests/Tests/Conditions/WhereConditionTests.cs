using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Conditions;

public class WhereConditionTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly TestFixture _fixture = new();
    
    [Fact]
    public void 等号演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Name == "John");
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"name\" = @p0;", sqlText);
        Assert.Equal([("@p0", "John")], parameters);
    }

    [Fact]
    public void 不等号演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Age != 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" <> @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void より大きい演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Age > 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" > @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void より小さい演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Age < 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" < @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void より大きいか等しい演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Age >= 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" >= @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void より小さいか等しい演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Age <= 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" <= @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void LIKE演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Name.Like("John%"));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"name\" LIKE @p0;", sqlText);
        Assert.Equal([("@p0", "John%")], parameters);
    }

    [Fact]
    public void AND演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Name == "John" && UserTable.Age > 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE (\"name\" = @p0 AND \"age\" > @p1);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }

    [Fact]
    public void OR演算子を使用してWHERE句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Where(UserTable.Name == "John" || UserTable.Age > 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE (\"name\" = @p0 OR \"age\" > @p1);", sqlText);
        Assert.Equal([("@p0", "John"), ("@p1", 20)], parameters);
    }
}
