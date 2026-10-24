namespace AuraORM;

/// <summary>
/// The SQLite rules of <c>docs/design.ja.md</c>.
/// </summary>
internal sealed class SqliteRenderer : DialectRenderer
{
    public override string Name => "SQLite";

    // SQLITE_MAX_VARIABLE_NUMBER defaults to 32766 from SQLite 3.32.0 (https://www.sqlite.org/limits.html).
    public override int MaxParameters => 32766;

    // §5.10: SQLITE_MAX_COMPOUND_SELECT, which defaults to 500.
    public override int MaxSetOperands => 500;

    protected override string LengthFunction => "LENGTH";

    // §3.3: a decimal is stored as TEXT, so comparing it by size, ordering by it, and MIN / MAX would order strings
    // ('9.5' > '10.25'), and SUM / AVG would compute in floating point. Equality is exact and allowed.
    public override void CheckDecimalOrdering(string operation) =>
        throw new InvalidOperationException(
            "SQLite stores decimal values as text, so "
                + operation
                + " of decimal values is not exact there: comparisons, ORDER BY, MIN, and MAX order them as strings, and SUM and AVG add them as floating-point numbers. On SQLite, compare decimals for equality only, or store the column as double or long."
        );

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
