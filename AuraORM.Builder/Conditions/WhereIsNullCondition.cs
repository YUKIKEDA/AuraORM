namespace AuraORM.Builder.Conditions;

/// <summary>
/// IS NULL演算子を使用したWHERE句の条件を表すクラス
/// </summary>
public class WhereIsNullCondition : WhereCondition
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="columnName">カラム名</param>
    public WhereIsNullCondition(string columnName)
        : base(columnName, "IS NULL", null)
    {
    }
}
