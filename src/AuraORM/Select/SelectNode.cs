namespace AuraORM;

/// <summary>
/// A SELECT statement in the statement tree (<c>docs/design.ja.md</c> §5). The clauses are written in the standard order,
/// whatever order the builder was called in.
/// </summary>
internal sealed class SelectNode(SelectState state) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        var tables = new TableAlias[state.Sources.Length];
        for (int i = 0; i < tables.Length; i++)
        {
            tables[i] = state.Sources[i].Table;
        }

        context.EnterScope(tables);
        RenderClauses(context);
        context.ExitScope();
    }

    private void RenderClauses(SqlRenderContext context)
    {
        context.Write(state.Distinct ? "SELECT DISTINCT " : "SELECT ");
        context.WriteList(state.Items);
        context.Write(" FROM ");
        foreach (SelectSourceState source in state.Sources)
        {
            context.Write(
                source.Kind switch
                {
                    JoinKind.From => string.Empty,
                    JoinKind.Inner => " INNER JOIN ",
                    JoinKind.Left => " LEFT JOIN ",
                    JoinKind.Right => " RIGHT JOIN ",
                    JoinKind.Full => " FULL JOIN ",
                    _ => " CROSS JOIN ",
                }
            );
            source.Table.RenderSource(context);
            if (source.On is not null)
            {
                context.Write(" ON ");
                source.On.Render(context);
            }
        }

        if (state.Where is not null)
        {
            context.Write(" WHERE ");
            state.Where.Render(context);
        }

        if (!state.OrderBy.IsEmpty)
        {
            context.Write(" ORDER BY ");
            for (int i = 0; i < state.OrderBy.Length; i++)
            {
                if (i > 0)
                {
                    context.Write(", ");
                }

                state.OrderBy[i].Node.Render(context);
                context.Write(state.OrderBy[i].Descending ? " DESC" : " ASC");
            }
        }

        if (state.Limit is not null || state.Offset is not null)
        {
            context.Dialect.RenderLimitOffset(context, state.Limit, state.Offset);
        }
    }
}
