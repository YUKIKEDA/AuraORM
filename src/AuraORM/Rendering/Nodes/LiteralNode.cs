namespace AuraORM;

/// <summary>
/// SQL text written by the caller: the literal parts of a raw SQL fragment, or the text of <see cref="Sql.Unsafe(string)"/>.
/// </summary>
internal sealed class LiteralNode(string text) : SqlNode
{
    public override void Render(SqlRenderContext context) => context.Write(text);
}
