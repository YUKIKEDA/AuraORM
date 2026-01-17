namespace AuraORM.Builder.Conditions;

/// <summary>
/// IS NOT NULL演算子を使用したWHERE句の条件を表すクラス
/// </summary>
public class WhereIsNotNullCondition : WhereCondition
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="columnName">カラム名</param>
    public WhereIsNotNullCondition(string columnName)
        : base(columnName, "IS NOT NULL", null)
    {
    }
}
