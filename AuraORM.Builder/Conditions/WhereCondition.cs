namespace AuraORM.Builder.Conditions;

/// <summary>
/// WHERE句の条件を表すクラス
/// </summary>
public class WhereCondition
{
    /// <summary>
    /// カラム名
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// 演算子
    /// </summary>
    public string Operator { get; }

    /// <summary>
    /// 値
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="columnName">カラム名</param>
    /// <param name="op">演算子</param>
    /// <param name="value">値</param>
    public WhereCondition(string columnName, string op, object? value)
    {
        ColumnName = columnName;
        Operator = op;
        Value = value;
    }

    /// <summary>
    /// AND条件で結合
    /// </summary>
    /// <param name="other">条件</param>
    /// <returns>複合条件</returns>
    public WhereCondition And(WhereCondition other)
    {
        return new WhereCompositeCondition(this, "AND", other);
    }

    /// <summary>
    /// OR条件で結合
    /// </summary>
    /// <param name="other">条件</param>
    /// <returns>複合条件</returns>
    public WhereCondition Or(WhereCondition other)
    {
        return new WhereCompositeCondition(this, "OR", other);
    }

}
