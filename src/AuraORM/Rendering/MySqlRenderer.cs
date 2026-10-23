namespace AuraORM;

/// <summary>
/// The MySQL and MariaDB rules of <c>docs/design.ja.md</c>. The two dialects share their expressions; they differ in
/// statements (RETURNING, upsert).
/// </summary>
internal sealed class MySqlRenderer(bool isMariaDb) : DialectRenderer
{
    public bool IsMariaDb { get; } = isMariaDb;

    public override string Name => IsMariaDb ? "MariaDB" : "MySQL";

    // ER_PS_MANY_PARAM: a prepared statement has at most 65535 placeholders.
    public override int MaxParameters => 65535;

    protected override char IdentifierOpen => '`';

    protected override char IdentifierClose => '`';

    public override string CastTypeName(CastTarget target) =>
        target switch
        {
            CastTarget.Int64 => "SIGNED",
            CastTarget.Double => "DOUBLE",
            CastTarget.Decimal => "DECIMAL(65,30)",
            CastTarget.String => "CHAR",
            CastTarget.DateTime => "DATETIME(6)",
            _ => "DATE",
        };

    // §4.2: || is OR in MySQL unless PIPES_AS_CONCAT is set.
    public override void RenderConcat(SqlRenderContext context, SqlNode[] arguments)
    {
        context.Write("CONCAT(");
        context.WriteList(arguments);
        context.Write(')');
    }

    // §4.2: MySQL's / returns a decimal even for integers.
    public override void RenderIntegerDivide(SqlRenderContext context, SqlNode left, SqlNode right) => RenderInfix(context, [left, right], " DIV ");

    // §4.2: <=> is MySQL's null-safe equality.
    public override void RenderDistinctFrom(SqlRenderContext context, DistinctFromNode node)
    {
        if (node.Distinct)
        {
            context.Write("NOT (");
        }

        node.Left.Render(context);
        context.Write(" <=> ");
        node.Right.Render(context);
        if (node.Distinct)
        {
            context.Write(')');
        }
    }

    // §4.2: SUM returns DECIMAL, so the sum is cast back to a signed integer.
    protected override void RenderSumOfIntegers(SqlRenderContext context, SqlNode argument)
    {
        context.Write("CAST(SUM(");
        argument.Render(context);
        context.Write(") AS SIGNED)");
    }

    // §4.2: AVG returns DECIMAL, so the average is cast to DOUBLE.
    protected override void RenderAverageAsDouble(SqlRenderContext context, SqlNode argument)
    {
        context.Write("CAST(AVG(");
        argument.Render(context);
        context.Write(") AS DOUBLE)");
    }
}
