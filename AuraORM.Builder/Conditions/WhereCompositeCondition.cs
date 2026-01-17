namespace AuraORM.Builder.Conditions;

/// <summary>
/// 複合条件を表すクラス
/// </summary>
public class WhereCompositeCondition : WhereCondition
{
    /// <summary>
    /// 左側の条件
    /// </summary>
    public WhereCondition Left { get; }

    /// <summary>
    /// 論理演算子
    /// </summary>
    public string LogicalOperator { get; }

    /// <summary>
    /// 右側の条件
    /// </summary>
    public WhereCondition Right { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="left">左側の条件</param>
    /// <param name="logicalOperator">論理演算子</param>
    /// <param name="right">右側の条件</param>
    public WhereCompositeCondition(WhereCondition left, string logicalOperator, WhereCondition right)
        : base(string.Empty, string.Empty, null)
    {
        Left = left;
        LogicalOperator = logicalOperator;
        Right = right;
    }
}
