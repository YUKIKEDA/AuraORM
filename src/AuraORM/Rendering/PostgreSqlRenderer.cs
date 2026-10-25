using System.Globalization;

namespace AuraORM;

/// <summary>
/// The PostgreSQL rules of <c>docs/design.ja.md</c>.
/// </summary>
internal sealed class PostgreSqlRenderer : DialectRenderer
{
    public override string Name => "PostgreSQL";

    public override DdlRenderer Ddl => field ??= new PostgreSqlDdlRenderer(this);

    // The protocol carries the parameter count as a 16-bit number; Npgsql rejects more than 65535.
    public override int MaxParameters => 65535;

    public override bool UsesPositionalParameters => true;

    protected override char IdentifierOpen => '"';

    protected override char IdentifierClose => '"';

    public override string CastTypeName(CastTarget target) =>
        target switch
        {
            CastTarget.Int64 => "bigint",
            CastTarget.Double => "double precision",
            CastTarget.Decimal => "numeric",
            CastTarget.String => "text",
            CastTarget.DateTime => "timestamp",
            _ => "date",
        };

    // §5.12: unnamed positional parameters, which Npgsql sends without rewriting the SQL.
    protected override string FormatParameterName(int index) => "$" + (index + 1).ToString(CultureInfo.InvariantCulture);

    // §4.2: SUM(bigint) is numeric, so the sum is cast back to bigint.
    protected override void RenderSumOfIntegers(SqlRenderContext context, SqlNode argument)
    {
        context.Write("CAST(SUM(");
        argument.Render(context);
        context.Write(") AS bigint)");
    }

    // §4.2: AVG of integers is numeric, so the argument is cast to double precision.
    protected override void RenderAverageAsDouble(SqlRenderContext context, SqlNode argument)
    {
        context.Write("AVG(");
        RenderCast(context, argument, "double precision");
        context.Write(')');
    }
}
