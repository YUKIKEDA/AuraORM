namespace AuraORM;

/// <summary>
/// A comparison that treats two nulls as equal. Each dialect writes it differently (<c>docs/design.ja.md</c> §4.2).
/// </summary>
internal sealed class DistinctFromNode(SqlNode left, SqlNode right, bool distinct) : SqlNode
{
    public SqlNode Left { get; } = left;

    public SqlNode Right { get; } = right;

    /// <summary>
    /// Gets a value indicating whether the node is <c>IS DISTINCT FROM</c> (<see langword="true"/>) or
    /// <c>IS NOT DISTINCT FROM</c> (<see langword="false"/>).
    /// </summary>
    public bool Distinct { get; } = distinct;

    public override void Render(SqlRenderContext context) => context.Dialect.RenderDistinctFrom(context, this);
}
