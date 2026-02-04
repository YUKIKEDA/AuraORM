using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class SelectQuerySQliteTests : IDisposable
{
    private static readonly UserTable UserTable = UserTable.Instance;

    private readonly DatabaseFixture _fixture;
    public SelectQuerySQliteTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _fixture.SeedUsers();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Selectで何もしていない場合はFrom句で指定したテーブルのデータを全件取得できる()
    {
        // Arrange & Act
        var query = _fixture.Db.Select().From(UserTable);
        var actual = await query.ToListAsync();

        // Assert
        Assert.Equal(4, actual.Count);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal("次郎", actual[2].Name);
        Assert.Equal("美咲", actual[3].Name);
    }

    [Fact]
    public async Task Selectで指定した1つのカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange
        var expected = new List<string> { "太郎", "花子", "次郎", "美咲" };

        // Act
        var query = _fixture.Db.Select(UserTable.Name).From(UserTable);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(expected, actual.Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task Selectで指定した複数のカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange & Act
        var query = _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Equal(4, actual.Count);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal(30, actual[1].Age);
        Assert.Equal("次郎", actual[2].Name);
        Assert.Equal(18, actual[2].Age);
        Assert.Equal("美咲", actual[3].Name);
        Assert.Equal(28, actual[3].Age);
    }
}
