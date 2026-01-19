namespace AuraORM.Builder.Conditions;

/// <summary>
/// JOIN句の種類
/// </summary>
public enum JoinType
{
    /// <summary>
    /// 内部結合
    /// </summary>
    Inner,
    /// <summary>
    /// 左外部結合
    /// </summary>
    Left,
    /// <summary>
    /// 右外部結合
    /// </summary>
    Right,
    /// <summary>
    /// 完全外部結合
    /// </summary>
    Full
}