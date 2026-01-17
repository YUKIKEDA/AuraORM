using System.Text;

using AuraORM.Builder.Table;

namespace AuraORM.Builder.Query;

/// <summary>
/// SELECTクエリを表すクラス
/// </summary>
public class SelectQuery
{
    private readonly DbContext? _context;
    private readonly Column[] _columns;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="context">データベースコンテキスト</param>
    /// <param name="columns">カラム</param>
    internal SelectQuery(DbContext? context = null, params Column[] columns)
    {
        _context = context;
        _columns = columns;
    }

    /// <summary>
    /// データベースコンテキストを取得します。
    /// </summary>
    /// <returns>データベースコンテキスト。関連付けられていない場合は null。</returns>
    public DbContext? GetContext() => _context;

    /// <summary>
    /// カラムを取得します。
    /// </summary>
    /// <returns>カラムの配列</returns>
    public Column[] GetColumns() => _columns;
}

/// <summary>
/// SELECTクエリビルダーを表すクラス
/// </summary>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TTable">テーブルの型</typeparam>
public sealed class SelectQuery<TEntity, TTable>where TTable : ITable
{
    private readonly TTable _table;
    private readonly DbContext? _dbContext;
    private readonly Column[]? _columns;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    /// <param name="columns">カラム</param>
    public SelectQuery(TTable table, DbContext? dbContext = null, Column[]? columns = null)
    {
        _table = table;
        _dbContext = dbContext;
        _columns = columns;
    }

    /// <summary>
    /// このビルダーに関連付けられているデータベースコンテキストを取得します。
    /// </summary>
    /// <returns>データベースコンテキスト。関連付けられていない場合は null。</returns>
    public DbContext? GetDbContext() => _dbContext;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public SelectQuery<TEntity, TTable> From(TTable table)
    {
        return new SelectQuery<TEntity, TTable>(table, _dbContext, _columns);
    }

    /// <summary>
    /// 構築されたSELECTクエリをSQLテキストとパラメータのリストに変換します。
    /// </summary>
    /// <returns>SQLテキストとパラメータのリストを含むタプル。</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build()
    {
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT ");
        
        if (_columns != null && _columns.Length > 0)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }
                sqlBuilder.Append(_columns[i].Name);
            }
        }
        else
        {
            sqlBuilder.Append("*");
        }
        
        sqlBuilder.Append(" FROM ");
        sqlBuilder.Append(_table.TableName);
        sqlBuilder.Append(";");
        
        var parameters = new List<(string Name, object? Value)>();
        return (sqlBuilder.ToString(), parameters);
    }
}