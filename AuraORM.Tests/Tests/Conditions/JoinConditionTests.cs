using AuraORM.Builder.Conditions;
using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Conditions;

public class JoinConditionTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private static readonly OrderTable OrderTable = OrderTable.Instance;
    private readonly TestFixture _fixture = new();

    [Fact]
    public void INNER_JOIN句を生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable).InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void LEFT_JOIN句を生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable).LeftJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" LEFT JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void RIGHT_JOIN句を生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable).RightJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" RIGHT JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void FULL_JOIN句を生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable).FullJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" FULL JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句とWHERE句を組み合わせて生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where(UserTable.Age > 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" WHERE \"age\" > @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void JOIN句とORDER_BY句を組み合わせて生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .OrderBy(new OrderByCondition("name", false));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" ORDER BY \"name\" ASC;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句とLIMIT句を組み合わせて生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Limit(10);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" LIMIT 10;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句とOFFSET句を組み合わせて生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Offset(5);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" OFFSET 5;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句とWHERE句とORDER_BY句を組み合わせて生成できる()
    {
        // Arrange
        var joinCondition = UserTable.Id == OrderTable.UserId;

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, joinCondition)
            .Where(UserTable.Age > 20)
            .OrderBy(new OrderByCondition("name", false));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" WHERE \"age\" > @p0 ORDER BY \"name\" ASC;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void JOIN句とWHERE句とLIMIT句とOFFSET句を組み合わせて生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where(UserTable.Age > 20)
            .Limit(10)
            .Offset(5);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" WHERE \"age\" > @p0 LIMIT 10 OFFSET 5;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void JOIN句で異なるカラムを結合条件として使用できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Name == OrderTable.ProductName);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"name\" = \"orders\".\"productname\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句で複数の条件を組み合わせて使用できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" WHERE (\"age\" > @p0 AND \"name\" <> @p1);", sqlText);
        Assert.Equal([("@p0", 20), ("@p1", "Bob")], parameters);
    }

    [Fact]
    public void JOIN句で1つのカラムを指定して取得できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select(UserTable.Name).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT \"name\" FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句で複数のカラムを指定して取得できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT \"name\", \"age\" FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句でJOIN先のテーブルのカラムを指定して取得できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select(OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT \"productname\" FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句で両方のテーブルのカラムを指定して取得できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select(UserTable.Name, OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT \"name\", \"productname\" FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void JOIN句でカラム指定とWHERE句を組み合わせて生成できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select(UserTable.Name, OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where(UserTable.Age > 20);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT \"name\", \"productname\" FROM \"users\" INNER JOIN \"orders\" ON \"users\".\"id\" = \"orders\".\"userid\" WHERE \"age\" > @p0;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }
}
