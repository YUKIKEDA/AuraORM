namespace AuraORM;

/// <summary>
/// <c>a [NOT] LIKE pattern [ESCAPE e]</c> (<c>docs/design.ja.md</c> §4.2). The same SQL on every dialect.
/// </summary>
internal sealed class LikeNode(SqlNode value, SqlNode pattern, bool negated, SqlNode? escape = null) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        value.Render(context);
        context.Write(negated ? " NOT LIKE " : " LIKE ");
        pattern.Render(context);
        if (escape is not null)
        {
            context.Write(" ESCAPE ");
            escape.Render(context);
        }
    }
}
