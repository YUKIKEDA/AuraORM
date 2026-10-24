namespace AuraORM;

/// <summary>
/// A <c>GROUP BY</c> key of the grouped statement used inside the subquery of <c>GroupScalar</c> or <c>GroupExists</c>
/// (<c>docs/design.ja.md</c> §5.11). It writes the key's column; anywhere else it throws, because only those subqueries see
/// the grouped statement's keys.
/// </summary>
internal sealed class OuterKeyNode(GroupKeyNode key) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        if (!context.InGroupSubquery)
        {
            throw new InvalidOperationException(
                "Outer(k) is used outside GroupScalar and GroupExists. It names a GROUP BY key of the grouped statement inside their subquery only."
            );
        }

        key.Render(context);
    }
}
