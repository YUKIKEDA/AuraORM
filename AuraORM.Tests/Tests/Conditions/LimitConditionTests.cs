using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Conditions;

public class LimitConditionTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly TestFixture _fixture = new();

    [Fact]
    public void LIMIT句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).Limit(10);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" LIMIT 10;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void WHERE句とLIMIT句を組み合わせて生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .Where(UserTable.Age > 20)
            .Limit(10);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" > @p0 LIMIT 10;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }

    [Fact]
    public void ORDER_BY句とLIMIT句を組み合わせて生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .OrderBy(new AuraORM.Builder.Conditions.OrderByCondition("name", false))
            .Limit(10);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" ORDER BY \"name\" ASC LIMIT 10;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void WHERE句とORDER_BY句とLIMIT句を組み合わせて生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .Where(UserTable.Age > 20)
            .OrderBy(new AuraORM.Builder.Conditions.OrderByCondition("name", false))
            .Limit(10);
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" > @p0 ORDER BY \"name\" ASC LIMIT 10;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }
}
