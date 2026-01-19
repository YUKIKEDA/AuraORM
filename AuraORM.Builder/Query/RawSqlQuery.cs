namespace AuraORM.Builder.Query;

/// <summary>
/// RAW SQLクエリビルダー
/// </summary>
public class RawSqlQuery
{
    private readonly string _rawSqlText;
    private readonly List<(string Name, object? Value)>? _parameters = [];
    private readonly DbContext? _dbContext;

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
        _dbContext = dbContext;
    }

    /// <summary>
    /// SQLテキストとパラメータのリストを取得します。
    /// </summary>
    /// <returns>SQLテキストとパラメータのリストを含むタプル</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build() => (_rawSqlText, _parameters ?? []);
}
