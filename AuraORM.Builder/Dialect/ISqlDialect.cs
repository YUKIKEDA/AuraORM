namespace AuraORM.Builder.Dialect;

/// <summary>
/// SQLデータベース間の方言の違いを吸収するインターフェース
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// 方言の名前（例: "SQLite", "PostgreSQL", "MySQL"）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// パラメータプレースホルダーを生成する（例: "@p0", "$1", "?"）
    /// </summary>
    /// <param name="index">パラメータのインデックス（0から開始）</param>
    /// <returns>プレースホルダー文字列</returns>
    string GetParameterPlaceholder(int index);

    /// <summary>
    /// 識別子（テーブル名・カラム名）をクォートする
    /// </summary>
    /// <param name="identifier">識別子</param>
    /// <returns>クォートされた識別子</returns>
    string QuoteIdentifier(string identifier);

    /// <summary>
    /// LIMIT/OFFSET句を生成する
    /// </summary>
    /// <param name="limit">取得件数（nullの場合は無制限）</param>
    /// <param name="offset">スキップ件数（nullの場合は0）</param>
    /// <returns>LIMIT/OFFSET句のSQL文字列</returns>
    string BuildLimitOffset(int? limit, int? offset);

    /// <summary>
    /// INSERT後に自動生成されたIDを取得するためのSQL句を生成する
    /// </summary>
    /// <param name="tableName">テーブル名</param>
    /// <param name="primaryKeyColumn">主キーカラム名</param>
    /// <returns>ID取得用のSQL句</returns>
    string GetLastInsertIdClause(string tableName, string primaryKeyColumn);

    /// <summary>
    /// 真偽値のリテラル表現を取得する
    /// </summary>
    /// <param name="value">真偽値</param>
    /// <returns>SQL表現</returns>
    string GetBooleanLiteral(bool value);

    /// <summary>
    /// 文字列連結演算子を取得する
    /// </summary>
    /// <returns>連結演算子（例: "||", "+"）</returns>
    string GetConcatOperator();

    /// <summary>
    /// 現在時刻を取得する関数を取得する
    /// </summary>
    /// <returns>現在時刻関数（例: "NOW()", "CURRENT_TIMESTAMP"）</returns>
    string GetCurrentTimestampFunction();
}
