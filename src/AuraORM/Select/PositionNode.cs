using System.Globalization;

namespace AuraORM;

/// <summary>
/// An item of the select list named by its position, as an <c>ORDER BY</c> key of a <c>SELECT DISTINCT</c>: <c>1</c>. The
/// number comes from the statement's shape, not from a value, so it is written as a literal (<c>docs/design.ja.md</c> §5.7).
/// </summary>
internal sealed class PositionNode(int position) : SqlNode
{
    public override void Render(SqlRenderContext context) => context.Write(position.ToString(CultureInfo.InvariantCulture));
}
