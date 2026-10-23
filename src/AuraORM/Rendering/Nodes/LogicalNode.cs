namespace AuraORM;

/// <summary>
/// <c>(a AND b AND …)</c> or <c>(a OR b OR …)</c>, always in parentheses (<c>docs/design.ja.md</c> §5.12).
/// </summary>
internal sealed class LogicalNode(bool isAnd, SqlNode[] operands) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        string separator = isAnd ? " AND " : " OR ";
        context.Write('(');
        for (int i = 0; i < operands.Length; i++)
        {
            if (i > 0)
            {
                context.Write(separator);
            }

            operands[i].Render(context);
        }

        context.Write(')');
    }
}
