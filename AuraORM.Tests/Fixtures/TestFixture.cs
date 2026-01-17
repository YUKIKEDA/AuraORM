using Microsoft.Data.Sqlite;
using AuraORM.Builder;

namespace AuraORM.Tests.Fixtures;

/// <summary>
/// テスト用のフィクスチャクラス
/// </summary>
public class TestFixture
{
    /// <summary>
    /// データベースコンテキスト
    /// </summary>
    public DbContext Db { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public TestFixture()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        var schema = new Schema();
        Db = new DbContext(connection, schema);
    }
}
