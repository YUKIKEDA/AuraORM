namespace AuraORM;

/// <summary>
/// <c>EXISTS (SELECT …)</c> (<c>docs/design.ja.md</c> §5.11).
/// </summary>
internal sealed class ExistsNode(SqlNode subquery) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.Write("EXISTS ");
        subquery.Render(context);
    }
}
