namespace AuraORM;

/// <summary>
/// <c>a BETWEEN low AND high</c>.
/// </summary>
internal sealed class BetweenNode(SqlNode value, SqlNode low, SqlNode high) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        value.Render(context);
        context.Write(" BETWEEN ");
        low.Render(context);
        context.Write(" AND ");
        high.Render(context);
    }
}
