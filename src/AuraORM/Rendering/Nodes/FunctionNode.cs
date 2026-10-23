namespace AuraORM;

/// <summary>
/// The scalar functions of <see cref="FunctionNode"/>.
/// </summary>
internal enum SqlFunction
{
    Concat,
    Lower,
    Upper,
    Length,
    Trim,
    Substring,
    Coalesce,
    NullIf,
    CurrentTimestamp,
}

/// <summary>
/// A scalar function call. Functions whose name or form differs by dialect are written by the dialect
/// (<c>docs/design.ja.md</c> §4.2).
/// </summary>
internal sealed class FunctionNode(SqlFunction function, SqlNode[] arguments) : SqlNode
{
    public SqlFunction Function { get; } = function;

    public SqlNode[] Arguments { get; } = arguments;

    public override void Render(SqlRenderContext context)
    {
        switch (Function)
        {
            case SqlFunction.Concat:
                context.Dialect.RenderConcat(context, Arguments);
                return;
            case SqlFunction.Length:
                context.Dialect.RenderLength(context, Arguments[0]);
                return;
            case SqlFunction.CurrentTimestamp:
                context.Write("CURRENT_TIMESTAMP");
                return;
            default:
                context.Write(
                    Function switch
                    {
                        SqlFunction.Lower => "LOWER",
                        SqlFunction.Upper => "UPPER",
                        SqlFunction.Trim => "TRIM",
                        SqlFunction.Substring => "SUBSTRING",
                        SqlFunction.Coalesce => "COALESCE",
                        _ => "NULLIF",
                    }
                );
                break;
        }

        context.Write('(');
        context.WriteList(Arguments);
        context.Write(')');
    }
}
