namespace AuraORM;

/// <summary>
/// <c>NOT (c)</c>.
/// </summary>
internal sealed class NotNode(SqlNode operand) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.Write("NOT (");
        operand.Render(context);
        context.Write(')');
    }
}
