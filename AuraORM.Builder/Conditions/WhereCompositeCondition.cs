namespace AuraORM.Builder.Conditions;

/// <summary>
/// 
/// </summary>
public class WhereCompositeCondition : WhereCondition
{
    /// <summary>
    /// 
    /// </summary>
    public WhereCondition Left { get; }

    /// <summary>
    /// 
    /// </summary>
    public string LogicalOperator { get; }

    /// <summary>
    /// 
    /// </summary>
    public WhereCondition Right { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="logicalOperator"></param>
    /// <param name="right"></param>
    public WhereCompositeCondition(WhereCondition left, string logicalOperator, WhereCondition right)
        : base(string.Empty, string.Empty, null)
    {
        Left = left;
        LogicalOperator = logicalOperator;
        Right = right;
    }
}
