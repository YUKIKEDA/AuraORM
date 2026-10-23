namespace AuraORM;

/// <summary>
/// The SQLite rules of <c>docs/design.ja.md</c>.
/// </summary>
internal sealed class SqliteRenderer : DialectRenderer
{
    public override string Name => "SQLite";

    // SQLITE_MAX_VARIABLE_NUMBER defaults to 32766 from SQLite 3.32.0 (https://www.sqlite.org/limits.html).
    public override int MaxParameters => 32766;

    protected override string LengthFunction => "LENGTH";

    protected override char IdentifierOpen => '"';

    protected override char IdentifierClose => '"';

    public override string CastTypeName(CastTarget target) =>
        target switch
        {
            CastTarget.Int64 => "INTEGER",
            CastTarget.Double => "REAL",
            _ => "TEXT",
        };

    // §4.2: SQLite writes null-safe equality as IS / IS NOT.
    public override void RenderDistinctFrom(SqlRenderContext context, DistinctFromNode node)
    {
        node.Left.Render(context);
        context.Write(node.Distinct ? " IS NOT " : " IS ");
        node.Right.Render(context);
    }
}
