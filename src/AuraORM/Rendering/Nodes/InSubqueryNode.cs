namespace AuraORM;

/// <summary>
/// <c>a IN (SELECT …)</c> or <c>a NOT IN (SELECT …)</c> (<c>docs/design.ja.md</c> §5.11).
/// </summary>
internal sealed class InSubqueryNode(SqlNode value, SqlNode subquery, bool negated) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        value.Render(context);
        context.Write(negated ? " NOT IN " : " IN ");
        subquery.Render(context);
    }
}
