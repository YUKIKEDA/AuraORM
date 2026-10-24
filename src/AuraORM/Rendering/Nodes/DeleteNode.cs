namespace AuraORM;

/// <summary>
/// A <c>DELETE</c> statement: <c>DELETE FROM "t" WHERE …</c> (<c>docs/design.ja.md</c> §8).
/// </summary>
/// <param name="target">The table.</param>
/// <param name="where">The condition, or <see langword="null"/> for <c>AllRows()</c>.</param>
internal sealed class DeleteNode(TableAlias target, SqlNode? where) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.EnterMutation(target);
        context.Write("DELETE FROM ");
        target.RenderTarget(context);
        if (where is not null)
        {
            context.Write(" WHERE ");
            where.Render(context);
        }

        context.ExitMutation();
    }
}
