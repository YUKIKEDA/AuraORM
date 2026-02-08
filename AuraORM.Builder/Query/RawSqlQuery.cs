namespace AuraORM.Builder.Query;

/// <summary>
/// RAW SQLクエリビルダー
/// </summary>
public class RawSqlQuery : IQueryBuilder
{
    private readonly string _rawSqlText;
    private readonly List<(string Name, object? Value)>? _parameters = [];

    /// <summary>
    /// データベースコンテキスト
    /// </summary>
    public DbContext DbContext { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="rawSqlText">SQLテキスト</param>
    /// <param name="parameters">パラメータ</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    public RawSqlQuery(string rawSqlText, List<(string Name, object? Value)>? parameters, DbContext? dbContext)
    {
        _rawSqlText = rawSqlText;
        _parameters = parameters;
        DbContext = dbContext!;
    }

    /// <summary>
    /// SQLテキストとパラメータのリストを取得します。
    /// </summary>
    /// <returns>SQLテキストとパラメータのリストを含むタプル</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build() => (_rawSqlText, _parameters ?? []);

    /// <summary>
    /// クエリを非同期で実行します。
    /// </summary>
    /// <param name="token">キャンセルトークン</param>
    /// <returns>影響を受けた行数</returns>
    /// <exception cref="InvalidOperationException">データベースコンテキストが未設定の場合</exception>
    public async Task<int> ExecuteAsync(CancellationToken token = default)
    {
        var db = DbContext
            ?? throw new InvalidOperationException("DbContext is required. Use db.RawSql(...) to create the query.");
        var connection = db.Connection;

        var (sql, parameters) = Build();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.TransactionContext?.Transaction;

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
