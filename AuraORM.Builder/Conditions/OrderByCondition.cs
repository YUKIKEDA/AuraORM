namespace AuraORM.Builder.Conditions;

/// <summary>
/// ORDER BY句の条件を表すクラス
/// </summary>
public class OrderByCondition
{
    /// <summary>
    /// カラム名
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// 降順かどうか
    /// </summary>
    public bool Descending { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="columnName">カラム名</param>
    /// <param name="descending">降順かどうか</param>
    public OrderByCondition(string columnName, bool descending)
    {
        ColumnName = columnName;
        Descending = descending;
    }
}
