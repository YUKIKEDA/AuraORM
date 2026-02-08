using System.Text;

using AuraORM.Builder.Dialect;
using AuraORM.Builder.Table;

namespace AuraORM.Builder.Query;

/// <summary>
/// INSERTクエリビルダー
/// </summary>
/// <typeparam name="TTable">テーブル</typeparam>
public sealed class InsertQuery<TTable> where TTable : ITable
{
    private readonly TTable _table;
    private readonly DbContext? _dbContext;

    private InsertQueryValues _values = [];

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    internal InsertQuery(TTable table, DbContext? dbContext = null)
    {
        _table = table;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 値を指定してINSERTクエリを生成します。
    /// </summary>
    /// <param name="values">値</param>
    /// <returns>INSERTクエリビルダー</returns>
    public InsertQuery<TTable> Values(InsertQueryValues values)
    {
        _values = values;
        return this;
    }

    /// <summary>
    /// SQLクエリをビルドします。
    /// </summary>
    /// <returns>SQLテキストとパラメータのタプル</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build()
    {
        var dialect = _dbContext?.Dialect ?? DialectProvider.Default;

        var sqlBuilder = new StringBuilder();
        var parameters = new List<(string Name, object? Value)>();

        // INSERT INTO句の構築
        sqlBuilder.Append("INSERT INTO ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_table.TableName));

        // 値が空の場合は特殊な形式を返す
        if (_values.Count() == 0)
        {
            sqlBuilder.Append(" () VALUES ();");
            return (sqlBuilder.ToString(), parameters);
        }

        // 最初の行からカラム名を取得
        var firstRow = _values.First();
        var columnNames = firstRow.Select(pair => pair.Column.Name).ToArray();

        // カラム名リストの構築
        sqlBuilder.Append(" (");
        for (int i = 0; i < columnNames.Length; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(", ");
            }
            sqlBuilder.Append(dialect.QuoteIdentifier(columnNames[i]));
        }
        sqlBuilder.Append(")");

        // VALUES句の構築
        sqlBuilder.Append(" VALUES ");
        int parameterIndex = 0;

        foreach (var row in _values)
        {
            if (parameterIndex > 0)
            {
                sqlBuilder.Append(", ");
            }

            sqlBuilder.Append("(");
            for (int i = 0; i < row.Length; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }

                var parameterName = dialect.GetParameterPlaceholder(parameterIndex);
                sqlBuilder.Append(parameterName);
                parameters.Add((parameterName, row[i].Value));
                parameterIndex++;
            }
            sqlBuilder.Append(")");
        }

        sqlBuilder.Append(";");

        return (sqlBuilder.ToString(), parameters);
    }

    /// <summary>
    /// クエリを非同期で実行します。
    /// </summary>
    /// <param name="token">キャンセルトークン</param>
    /// <returns>影響を受けた行数</returns>
    /// <exception cref="InvalidOperationException">データベースコンテキストが未設定の場合</exception>
    public async Task<int> ExecuteAsync(CancellationToken token = default)
    {
        var db = _dbContext 
            ?? throw new InvalidOperationException("DbContext is required. Use db.Insert(...).Set(...) to create the query.");
        var connection = db.Connection;
        
        var (sql, parameters) = Build();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _dbContext.TransactionContext?.Transaction;
            
        foreach (var (name, value) in parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            command.Parameters.Add(param);
        }
            
        return await command.ExecuteNonQueryAsync(token);
    }
}