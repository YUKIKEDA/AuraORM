namespace AuraORM;

/// <summary>
/// The operators of <see cref="ComparisonNode"/>.
/// </summary>
internal enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}

/// <summary>
/// A comparison: <c>a = b</c>. Comparisons are not written in parentheses (<c>docs/design.ja.md</c> §5.12).
/// </summary>
internal sealed class ComparisonNode(ComparisonOperator op, SqlNode left, SqlNode right) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        left.Render(context);
        context.Write(
            op switch
            {
                ComparisonOperator.Equal => " = ",
                ComparisonOperator.NotEqual => " <> ",
                ComparisonOperator.GreaterThan => " > ",
                ComparisonOperator.GreaterThanOrEqual => " >= ",
                ComparisonOperator.LessThan => " < ",
                _ => " <= ",
            }
        );
        right.Render(context);
    }
}
