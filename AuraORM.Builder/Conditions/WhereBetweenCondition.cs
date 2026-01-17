namespace AuraORM.Builder.Conditions;

/// <summary>
/// BETWEEN演算子を使用したWHERE句の条件を表すクラス
/// </summary>
internal class WhereBetweenCondition : WhereCondition
{
    /// <summary>
    /// 最小値
    /// </summary>
    public object? Min { get; }

    /// <summary>
    /// 最大値
    /// </summary>
    public object? Max { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="columnName">カラム名</param>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値</param>
    public WhereBetweenCondition(string columnName, object? min, object? max)
        : base(columnName, "BETWEEN", null)
    {
        Min = min;
        Max = max;
    }
}
