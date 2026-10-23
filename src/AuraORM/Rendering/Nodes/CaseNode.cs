namespace AuraORM;

/// <summary>
/// <c>CASE WHEN c THEN v … ELSE v END</c>.
/// </summary>
internal sealed class CaseNode((SqlNode When, SqlNode Then)[] branches, SqlNode otherwise) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.Write("CASE");
        foreach ((SqlNode when, SqlNode then) in branches)
        {
            context.Write(" WHEN ");
            when.Render(context);
            context.Write(" THEN ");
            then.Render(context);
        }

        context.Write(" ELSE ");
        otherwise.Render(context);
        context.Write(" END");
    }
}
