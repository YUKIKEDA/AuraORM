namespace AuraORM;

/// <summary>
/// The target types of <c>CAST</c> (<c>docs/design.ja.md</c> §4.3).
/// </summary>
internal enum CastTarget
{
    Int64,
    Double,
    Decimal,
    String,
    DateTime,
    DateOnly,
}

/// <summary>
/// <c>CAST(a AS type)</c>, with the type name of the dialect.
/// </summary>
internal sealed class CastNode(SqlNode value, CastTarget target) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.Write("CAST(");
        value.Render(context);
        context.Write(" AS ");
        context.Write(context.Dialect.CastTypeName(target));
        context.Write(')');
    }
}
