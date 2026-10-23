namespace AuraORM;

/// <summary>
/// The negation of a number: <c>(-a)</c>.
/// </summary>
internal sealed class NegateNode(SqlNode operand) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.Write("(-");
        operand.Render(context);
        context.Write(')');
    }
}
