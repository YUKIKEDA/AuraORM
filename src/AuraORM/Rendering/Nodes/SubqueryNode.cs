namespace AuraORM;

/// <summary>
/// A SELECT statement inside another: <c>(SELECT …)</c> (<c>docs/design.ja.md</c> §5.11). Its dialect must be the dialect of
/// the statement it is written in, because the rules of each dialect (§5.9) were applied when it was built.
/// </summary>
internal sealed class SubqueryNode(SelectState state, DialectRenderer dialect) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        if (!ReferenceEquals(context.Dialect, dialect))
        {
            throw new InvalidOperationException(
                "A subquery built for "
                    + dialect.Name
                    + " is used in a "
                    + context.Dialect.Name
                    + " statement. Build the subquery with the same dialect."
            );
        }

        context.CountSubquery();
        context.Write('(');
        new SelectNode(state).Render(context);
        context.Write(')');
    }
}
