using AuraORM.Builder.Conditions;
using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Conditions;

public class OrderByConditionTests
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private readonly TestFixture _fixture = new();

    [Fact]
    public void 昇順でORDER_BY句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).OrderBy(new OrderByCondition("name", false));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" ORDER BY \"name\" ASC;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void 降順でORDER_BY句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable).OrderBy(new OrderByCondition("age", true));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" ORDER BY \"age\" DESC;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void 複数のORDER_BY句を生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .OrderBy(new OrderByCondition("name", false))
            .OrderBy(new OrderByCondition("age", true));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" ORDER BY \"name\" ASC, \"age\" DESC;", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void WHERE句とORDER_BY句を組み合わせて生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .Where(UserTable.Age > 20)
            .OrderBy(new OrderByCondition("name", false));
        var (sqlText, parameters) = query.Build();

        // Assert
        Assert.Equal("SELECT * FROM \"users\" WHERE \"age\" > @p0 ORDER BY \"name\" ASC;", sqlText);
        Assert.Equal([("@p0", 20)], parameters);
    }
}
