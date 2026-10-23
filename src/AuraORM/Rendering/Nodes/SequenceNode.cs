namespace AuraORM;

/// <summary>
/// Nodes written one after another with nothing between them: the parts of a raw SQL fragment.
/// </summary>
internal sealed class SequenceNode(SqlNode[] parts) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        foreach (SqlNode part in parts)
        {
            part.Render(context);
        }
    }
}
