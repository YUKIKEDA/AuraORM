namespace AuraORM;

/// <summary>
/// <c>a IS NULL</c> or <c>a IS NOT NULL</c>.
/// </summary>
internal sealed class IsNullNode(SqlNode value, bool negated) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        value.Render(context);
        context.Write(negated ? " IS NOT NULL" : " IS NULL");
    }
}
