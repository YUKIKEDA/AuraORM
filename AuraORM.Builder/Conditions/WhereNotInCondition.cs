namespace AuraORM.Builder.Conditions;

/// <summary>
/// NOT IN演算子を使用したWHERE句の条件を表すクラス
/// </summary>
internal class WhereNotInCondition : WhereCondition
{
    /// <summary>
    /// 値の配列
    /// </summary>
    public object[] Values { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="columnName">カラム名</param>
    /// <param name="values">値の配列</param>
    public WhereNotInCondition(string columnName, object[] values)
        : base(columnName, "NOT IN", null)
    {
        Values = values;
    }
}
