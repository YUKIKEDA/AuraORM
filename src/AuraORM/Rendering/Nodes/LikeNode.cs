namespace AuraORM;

/// <summary>
/// <c>a LIKE pattern</c>. Case sensitivity follows the database's collation.
/// </summary>
internal sealed class LikeNode(SqlNode value, SqlNode pattern, bool negated) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        value.Render(context);
        context.Write(negated ? " NOT LIKE " : " LIKE ");
        pattern.Render(context);
    }
}
