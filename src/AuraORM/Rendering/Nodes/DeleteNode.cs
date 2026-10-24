using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// A <c>DELETE</c> statement: <c>DELETE FROM "t" WHERE …</c> (<c>docs/design.ja.md</c> §8).
/// </summary>
/// <param name="target">The table.</param>
/// <param name="where">The condition, or <see langword="null"/> for <c>AllRows()</c>.</param>
/// <param name="returning">The items of <c>Returning</c>, or an empty array (§9).</param>
internal sealed class DeleteNode(TableAlias target, SqlNode? where, ImmutableArray<SqlNode> returning) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.EnterMutation(target);
        context.Write("DELETE FROM ");
        target.RenderTarget(context);

        // §9: SQL Server's OUTPUT goes after the table and before WHERE; RETURNING goes at the end.
        bool output = context.Dialect.ReturnsWithOutput;
        if (output)
        {
            context.WriteReturning(target, returning, "DELETED");
        }

        if (where is not null)
        {
            context.Write(" WHERE ");
            where.Render(context);
        }

        if (!output)
        {
            context.WriteReturning(target, returning, null);
        }

        context.ExitMutation();
    }
}
