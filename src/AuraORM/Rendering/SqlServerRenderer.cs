namespace AuraORM;

/// <summary>
/// The SQL Server rules of <c>docs/design.ja.md</c>.
/// </summary>
internal sealed class SqlServerRenderer : DialectRenderer
{
    public override string Name => "SQL Server";

    // An RPC request carries at most 2100 parameters.
    public override int MaxParameters => 2100;

    // §4.2: LEN returns bigint for the max types (nvarchar(max) is the default string column), so the result is cast to the
    // int that Length reads on every dialect.
    public override void RenderLength(SqlRenderContext context, SqlNode argument)
    {
        context.Write("CAST(LEN(");
        argument.Render(context);
        context.Write(") AS int)");
    }

    protected override char IdentifierOpen => '[';

    protected override char IdentifierClose => ']';

    protected override string CountFunction => "COUNT_BIG";

    public override string CastTypeName(CastTarget target) =>
        target switch
        {
            CastTarget.Int64 => "bigint",
            CastTarget.Double => "float",
            CastTarget.Decimal => "decimal(38,18)",
            CastTarget.String => "nvarchar(max)",
            CastTarget.DateTime => "datetime2",
            _ => "date",
        };

    // §4.2: + concatenates strings; CONCAT would turn NULL into an empty string.
    public override void RenderConcat(SqlRenderContext context, SqlNode[] arguments) => RenderInfix(context, arguments, " + ");

    // §4.2: IS [NOT] DISTINCT FROM is from SQL Server 2022, so 2019 needs the expansion. Each operand is written twice,
    // and a value gets a parameter each time.
    public override void RenderDistinctFrom(SqlRenderContext context, DistinctFromNode node)
    {
        if (node.Distinct)
        {
            context.Write("NOT ");
        }

        // §4.2: two-valued even when only one side is null; (a = b OR (a IS NULL AND b IS NULL)) would be UNKNOWN there.
        context.Write("((");
        node.Left.Render(context);
        context.Write(" IS NULL AND ");
        node.Right.Render(context);
        context.Write(" IS NULL) OR (");
        node.Left.Render(context);
        context.Write(" IS NOT NULL AND ");
        node.Right.Render(context);
        context.Write(" IS NOT NULL AND ");
        node.Left.Render(context);
        context.Write(" = ");
        node.Right.Render(context);
        context.Write("))");
    }

    // §4.2: SUM(int) is int and overflows, so each value is cast to bigint first.
    protected override void RenderSumOfIntegers(SqlRenderContext context, SqlNode argument)
    {
        context.Write("SUM(");
        RenderCast(context, argument, "bigint");
        context.Write(')');
    }

    // §4.2: AVG(int) is int, so each value is cast to float first.
    protected override void RenderAverageAsDouble(SqlRenderContext context, SqlNode argument)
    {
        context.Write("AVG(");
        RenderCast(context, argument, "float");
        context.Write(')');
    }
}
