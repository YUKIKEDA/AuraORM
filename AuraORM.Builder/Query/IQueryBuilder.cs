namespace AuraORM.Builder.Query;

public interface IQueryBuilder
{
    /// <summary>
    /// データベースコンテキスト
    /// </summary>
    DbContext DbContext { get; }

    /// <summary>
    /// 構築されたクエリをSQLテキストとパラメータのリストに変換
    /// </summary>
    (string SqlText, List<(string Name, object? Value)> Parameters) Build();
}
