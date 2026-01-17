namespace AuraORM.Tests.Tests.Query;

public class SelectQueryTests
{
    [Fact]
    public void Selectで何もしていない場合はFrom句で指定したテーブルのデータを全件取得できる()
    {
        // Arrange

        // Act
        // _fixture.Db.Select().From(UserTable).ToListAsync();

        // Assert
    }

    [Fact]
    public void Selectで指定した1つのカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange

        // Act
        // _fixture.Db.Select(UserTable.Name).From(UserTable).ToArrayAsync();

        // Assert
    }

    [Fact]
    public void Selectで指定した複数のカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange
        
        // Act
        // _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable).ToArrayAsync();
        
        // Assert
    }
}
