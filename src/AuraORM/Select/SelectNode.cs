using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// A SELECT statement in the statement tree (<c>docs/design.ja.md</c> §5). The clauses are written in the standard order,
/// whatever order the builder was called in. Inside a named query, each item is written with its name (§5.11).
/// </summary>
internal sealed class SelectNode(SelectState state, ImmutableArray<string> columnNames = default) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        int depth = context.EnterStatement();
        if (!state.Ctes.IsEmpty)
        {
            if (depth > 1)
            {
                // §5.11: SQL Server rejects a WITH inside a subquery, a derived table, or a set operation's operand.
                throw new InvalidOperationException(
                    "A statement with With is used inside another statement; only the outermost statement has a WITH. List all the named queries in its With."
                );
            }

            context.Write("WITH ");
            for (int i = 0; i < state.Ctes.Length; i++)
            {
                if (i > 0)
                {
                    context.Write(", ");
                }

                context.WriteCte(state.Ctes[i]);
            }

            context.Write(' ');
        }

        RenderStatement(context);
        context.ExitStatement();
    }

    private void RenderStatement(SqlRenderContext context)
    {
        var tables = new TableAlias[state.Sources.Length];
        for (int i = 0; i < tables.Length; i++)
        {
            tables[i] = state.Sources[i].Table;
        }

        context.EnterScope(tables);
        bool inHaving = context.InHaving;
        context.InHaving = false;
        RenderCore(context);
        if (state.SetOperands.IsEmpty)
        {
            RenderTail(context);
            context.InHaving = inHaving;
            context.ExitScope();
            return;
        }

        if (state.SetOperands.Length + 1 > context.Dialect.MaxSetOperands)
        {
            ThrowHelper.ThrowTooManySetOperands(state.SetOperands.Length + 1, context.Dialect.MaxSetOperands, context.Dialect.Name);
        }

        // §5.10: the operands follow without parentheses, each in its own scope, and ORDER BY and LIMIT apply to the whole
        // compound SELECT; its ORDER BY keys are positions, so they need no table.
        context.ExitScope();
        foreach (SetOperand operand in state.SetOperands)
        {
            context.Write(
                operand.Operator switch
                {
                    SetOperator.Union => " UNION ",
                    SetOperator.UnionAll => " UNION ALL ",
                    SetOperator.Intersect => " INTERSECT ",
                    _ => " EXCEPT ",
                }
            );
            new SelectNode(operand.State, columnNames).Render(context);
        }

        RenderTail(context);
        context.InHaving = inHaving;
    }

    // §5.11: the items of a named query are named, so that the statement around it can name its columns.
    private void WriteNamedItems(SqlRenderContext context)
    {
        if (columnNames.Length != state.Items.Length)
        {
            throw new InvalidOperationException(
                "A named query selects " + state.Items.Length + " items for " + columnNames.Length + " column names."
            );
        }

        for (int i = 0; i < state.Items.Length; i++)
        {
            if (i > 0)
            {
                context.Write(", ");
            }

            state.Items[i].Render(context);
            context.Write(" AS ");
            context.WriteIdentifier(columnNames[i]);
        }
    }

    private void RenderCore(SqlRenderContext context)
    {
        context.Write(state.Distinct ? "SELECT DISTINCT " : "SELECT ");
        if (columnNames.IsDefaultOrEmpty)
        {
            context.WriteList(state.Items);
        }
        else
        {
            WriteNamedItems(context);
        }

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

        if (!state.GroupBy.IsEmpty)
        {
            context.Write(" GROUP BY ");
            for (int i = 0; i < state.GroupBy.Length; i++)
            {
                if (i > 0)
                {
                    context.Write(", ");
                }

                // §5.5: SQL Server needs a column in each key, and MySQL reads a key of one value as a column position.
                int columns = context.ColumnsWritten;
                int subqueries = context.SubqueriesWritten;
                state.GroupBy[i].Render(context);
                if (context.SubqueriesWritten != subqueries)
                {
                    throw new InvalidOperationException(
                        "A GROUP BY key holds a subquery. SQL Server rejects it, and MySQL rejects a correlated one; group by a column expression."
                    );
                }

                if (context.ColumnsWritten == columns)
                {
                    throw new InvalidOperationException(
                        "A GROUP BY key has no column. SQL Server rejects such a key, and MySQL and MariaDB read a key of one value as a column position; group by an expression of a column."
                    );
                }
            }
        }

        if (state.Having is not null)
        {
            context.Write(" HAVING ");
            context.InHaving = true;
            state.Having.Render(context);
            context.InHaving = false;
        }
    }

    private void RenderTail(SqlRenderContext context)
    {
        if (!state.OrderBy.IsEmpty)
        {
            context.Write(" ORDER BY ");
            for (int i = 0; i < state.OrderBy.Length; i++)
            {
                if (i > 0)
                {
                    context.Write(", ");
                }

                // §5.8: MySQL and MariaDB read a key of one value as a column position, and SQL Server rejects it.
                if (state.OrderBy[i].Node.Unwrapped is ParameterNode)
                {
                    throw new InvalidOperationException(
                        "An ORDER BY key is only a value. MySQL and MariaDB read it as a column position, and SQL Server rejects it; order by an expression of a column."
                    );
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
