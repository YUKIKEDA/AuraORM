namespace AuraORM.Builder.Conditions;

/// <summary>
/// JOIN条件を表すクラス
/// </summary>
public class JoinCondition
{
    /// <summary>
    /// 左辺のカラム名
    /// </summary>
    public string LeftColumnName { get; }

    /// <summary>
    /// 右辺のカラム名
    /// </summary>
    public string RightColumnName { get; }

    public JoinCondition(string leftColumnName, string rightColumnName)
    {
        LeftColumnName = leftColumnName;
        RightColumnName = rightColumnName;
    }
}
