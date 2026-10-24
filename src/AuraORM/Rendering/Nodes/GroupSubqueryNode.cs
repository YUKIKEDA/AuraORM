namespace AuraORM;

/// <summary>
/// The subquery of <c>GroupScalar</c> or <c>GroupExists</c>, in which the grouped statement's columns can be used only
/// through its <c>GROUP BY</c> keys (<c>docs/design.ja.md</c> §5.11).
/// </summary>
internal sealed class GroupSubqueryNode(SqlNode subquery) : SqlNode
{
    public override void Render(SqlRenderContext context) => context.WriteGroupSubquery(subquery);
}
