using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Query;

public class SelectQueryTests
{
    private static readonly UserTable UserTable = UserTable.Instance;

    private readonly TestFixture _fixture = new();

    [Fact]
    public void Selectで何もしていない場合はFrom句で指定したテーブルのデータを全件取得するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select().From(UserTable);
        var (sqlText, parameters) = query.Build();

        // Assert
        // SQLテキストが SELECT * FROM "users"; になる
        Assert.Equal("SELECT * FROM \"users\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Selectで指定した1つのカラムのデータをFromで指定したテーブルから取得するSQLが生成できる()
    {
        // Arrange

        // Act
        var query = _fixture.Db.Select(UserTable.Name).From(UserTable);
        var (sqlText, parameters) = query.Build();

        // Assert
        // SQLテキストが SELECT "name" FROM "users"; になる
        Assert.Equal("SELECT \"name\" FROM \"users\";", sqlText);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Selectで指定した複数のカラムのデータをFromで指定したテーブルから取得するSQLが生成できる()
    {
        // Arrange
        
        // Act
        var query = _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable);
        var (sqlText, parameters) = query.Build();
        
        // Assert
        // SQLテキストが SELECT "name", "age" FROM "users"; になる
        Assert.Equal("SELECT \"name\", \"age\" FROM \"users\";", sqlText);
        Assert.Empty(parameters);
    }
}
