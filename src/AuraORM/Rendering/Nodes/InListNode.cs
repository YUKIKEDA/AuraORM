namespace AuraORM;

/// <summary>
/// <c>a IN (@p0, @p1, …)</c>. An empty list is written <c>1 = 0</c> (and <c>1 = 1</c> for <c>NOT IN</c>),
/// because <c>IN ()</c> is not valid SQL (<c>docs/design.ja.md</c> §4.2).
/// </summary>
internal sealed class InListNode(SqlNode value, SqlNode[] items, bool negated) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        if (items.Length == 0)
        {
            context.Write(negated ? "1 = 1" : "1 = 0");
            return;
        }

        value.Render(context);
        context.Write(negated ? " NOT IN (" : " IN (");
        context.WriteList(items);
        context.Write(')');
    }
}
