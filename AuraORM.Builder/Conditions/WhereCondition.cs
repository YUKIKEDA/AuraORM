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
    /// AND演算子をオーバーロードします。
    /// </summary>
    /// <param name="left">左側の条件</param>
    /// <param name="right">右側の条件</param>
    /// <returns>複合条件</returns>
    public static WhereCondition operator &(WhereCondition left, WhereCondition right)
    {
        return new WhereCompositeCondition(left, "AND", right);
    }

    /// <summary>
    /// OR演算子をオーバーロードします。
    /// </summary>
    /// <param name="left">左側の条件</param>
    /// <param name="right">右側の条件</param>
    /// <returns>複合条件</returns>
    public static WhereCondition operator |(WhereCondition left, WhereCondition right)
    {
        return new WhereCompositeCondition(left, "OR", right);
    }

    /// <summary>
    /// true演算子をオーバーロードします（&&演算子で使用されます）。
    /// </summary>
    /// <param name="condition">条件</param>
    /// <returns>短絡評価を防ぐため常にfalse</returns>
    public static bool operator true(WhereCondition condition)
    {
        return false;
    }

    /// <summary>
    /// false演算子をオーバーロードします（&&演算子で使用されます）。
    /// </summary>
    /// <param name="condition">条件</param>
    /// <returns>常にfalse</returns>
    public static bool operator false(WhereCondition condition)
    {
        return false;
    }

}
