using AuraORM.Builder.Table;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Generators;

/// <summary>
/// ソースジェネレータで生成されたファイルのテスト
/// </summary>
public class TableSourceGeneratorTests
{
    /// <summary>
    /// UserTableが正しく生成されていることを確認する
    /// </summary>
    [Fact]
    public void UserTable_ShouldBeGeneratedCorrectly()
    {
        // Instanceがnullでないことを確認
        Assert.NotNull(UserTable.Instance);
        
        // 同じインスタンスであることを確認（シングルトン）
        Assert.Same(UserTable.Instance, UserTable.Instance);
    }

    /// <summary>
    /// TableNameが正しいことを確認する
    /// </summary>
    [Fact]
    public void UserTable_TableName_ShouldBeCorrect()
    {
        var table = UserTable.Instance;
        Assert.Equal("users", table.TableName);
    }

    /// <summary>
    /// ColumnNamesが正しいことを確認する
    /// </summary>
    [Fact]
    public void UserTable_ColumnNames_ShouldBeCorrect()
    {
        var table = UserTable.Instance;
        var expectedColumns = new[] { "id", "name", "age", "createdat" };
        
        Assert.Equal(expectedColumns, table.ColumnNames);
        Assert.Equal(4, table.ColumnNames.Length);
    }

    /// <summary>
    /// PrimaryKeyColumnNameが正しいことを確認する
    /// </summary>
    [Fact]
    public void UserTable_PrimaryKeyColumnName_ShouldBeCorrect()
    {
        var table = UserTable.Instance;
        Assert.Equal("id", table.PrimaryKeyColumnName);
        Assert.NotNull(table.PrimaryKeyColumnName);
    }

    /// <summary>
    /// IsAutoIncrementが正しいことを確認する
    /// </summary>
    [Fact]
    public void UserTable_IsAutoIncrement_ShouldBeTrue()
    {
        var table = UserTable.Instance;
        Assert.True(table.IsAutoIncrement);
    }

    /// <summary>
    /// InsertColumnNamesが正しいことを確認する（AutoIncrement列を除く）
    /// </summary>
    [Fact]
    public void UserTable_InsertColumnNames_ShouldExcludeAutoIncrement()
    {
        var table = UserTable.Instance;
        var expectedInsertColumns = new[] { "name", "age", "createdat" };
        
        Assert.Equal(expectedInsertColumns, table.InsertColumnNames);
        Assert.Equal(3, table.InsertColumnNames.Length);
        
        // AutoIncrement列（id）が含まれていないことを確認
        Assert.DoesNotContain("id", table.InsertColumnNames);
    }

    /// <summary>
    /// 各カラムの静的フィールドが正しく定義されていることを確認する
    /// </summary>
    [Fact]
    public void UserTable_ColumnFields_ShouldBeDefinedCorrectly()
    {
        // Idカラム
        Assert.NotNull(UserTable.Id);
        Assert.Equal("id", UserTable.Id.Name);
        Assert.Equal(0, UserTable.Id.Ordinal);
        Assert.IsType<Column<int>>(UserTable.Id);

        // Nameカラム
        Assert.NotNull(UserTable.Name);
        Assert.Equal("name", UserTable.Name.Name);
        Assert.Equal(1, UserTable.Name.Ordinal);
        Assert.IsType<Column<string>>(UserTable.Name);

        // Ageカラム
        Assert.NotNull(UserTable.Age);
        Assert.Equal("age", UserTable.Age.Name);
        Assert.Equal(2, UserTable.Age.Ordinal);
        Assert.IsType<Column<int?>>(UserTable.Age);

        // CreatedAtカラム
        Assert.NotNull(UserTable.CreatedAt);
        Assert.Equal("createdat", UserTable.CreatedAt.Name);
        Assert.Equal(3, UserTable.CreatedAt.Ordinal);
        Assert.IsType<Column<DateTime>>(UserTable.CreatedAt);
    }

    /// <summary>
    /// ITableインターフェースを正しく実装していることを確認する
    /// </summary>
    [Fact]
    public void UserTable_ShouldImplementITable()
    {
        var table = UserTable.Instance;
        Assert.IsAssignableFrom<ITable>(table);
    }

    /// <summary>
    /// カラムの順序が正しいことを確認する
    /// </summary>
    [Fact]
    public void UserTable_ColumnOrdinals_ShouldBeSequential()
    {
        Assert.Equal(0, UserTable.Id.Ordinal);
        Assert.Equal(1, UserTable.Name.Ordinal);
        Assert.Equal(2, UserTable.Age.Ordinal);
        Assert.Equal(3, UserTable.CreatedAt.Ordinal);
    }

    /// <summary>
    /// ColumnNamesと実際のカラムフィールドの名前が一致することを確認する
    /// </summary>
    [Fact]
    public void UserTable_ColumnNames_ShouldMatchColumnFields()
    {
        var table = UserTable.Instance;
        var columnFields = new Column[]
        {
            UserTable.Id,
            UserTable.Name,
            UserTable.Age,
            UserTable.CreatedAt
        };

        for (int i = 0; i < table.ColumnNames.Length; i++)
        {
            Assert.Equal(table.ColumnNames[i], columnFields[i].Name);
        }
    }
}