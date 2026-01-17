namespace AuraORM.Builder.Conditions;

/// <summary>
/// IN演算子を使用したWHERE句の条件を表すクラス
/// </summary>
public class WhereInCondition : WhereCondition
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
    public WhereInCondition(string columnName, object[] values)
        : base(columnName, "IN", null)
    {
        Values = values;
    }
}
