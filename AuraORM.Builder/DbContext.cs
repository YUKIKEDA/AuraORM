using System.Data.Common;

using AuraORM.Builder.Dialect;
using AuraORM.Builder.Query;
using AuraORM.Builder.Table;

namespace AuraORM.Builder;

/// <summary>
/// データベースコンテキストを表すクラス
/// </summary>
public class DbContext
{
    /// <summary>
    /// データベース接続
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// スキーマ
    /// </summary>
    public Schema Schema { get; }

    /// <summary>
    /// SQL方言
    /// </summary>
    public ISqlDialect Dialect { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="connection">データベース接続</param>
    /// <param name="schema">スキーマ</param>
    public DbContext(DbConnection connection, Schema schema, ISqlDialect? sqlDialect = null)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Dialect = sqlDialect ?? DialectProvider.Default;
    }

    /// <summary>
    /// SELECTクエリを生成します。
    /// </summary>
    /// <param name="columns">カラム</param>
    /// <returns>SELECTクエリビルダー</returns>
    public SelectQuery Select(params Column[] columns)
    {
        return new SelectQuery(this, columns);
    }

    /// <summary>
    /// INSERTクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>INSERTクエリビルダー</returns>
    public InsertQuery<TTable> Insert<TTable>(TTable table) where TTable : ITable
    {
        return new InsertQuery<TTable>(table, this);
    }

    /// <summary>
    /// UPDATEクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>UPDATEクエリビルダー</returns>
    public UpdateQuery<TTable> Update<TTable>(TTable table) where TTable : ITable
    {
        return new UpdateQuery<TTable>(table, this);
    }
}
