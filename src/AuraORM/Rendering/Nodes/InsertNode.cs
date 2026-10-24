using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>INSERT</c> statement: <c>INSERT INTO "t" ("a", "b") VALUES (@p0, @p1), …</c> (<c>docs/design.ja.md</c> §6.2). The
/// rows were checked when the statement was built: there is at least one, all set the same columns, and rows that set no
/// column are several only where the dialect writes <c>() VALUES (), ()</c>.
/// </summary>
/// <param name="target">The table.</param>
/// <param name="columns">The names of the columns that the rows set, in declaration order.</param>
/// <param name="rows">The values of each row, in the order of <paramref name="columns"/>.</param>
internal sealed class InsertNode(TableAlias target, ImmutableArray<string> columns, ImmutableArray<ImmutableArray<ParameterNode>> rows) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.Write("INSERT INTO ");
        target.RenderTarget(context);
        if (columns.IsEmpty)
        {
            if (!context.Dialect.WritesEmptyValueRows)
            {
                context.Write(" DEFAULT VALUES");
                return;
            }

            context.Write(" () VALUES ");
            for (int i = 0; i < rows.Length; i++)
            {
                context.Write(i == 0 ? "()" : ", ()");
            }

            return;
        }

        context.Write(" (");
        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                context.Write(", ");
            }

            context.WriteIdentifier(columns[i]);
        }

        context.Write(") VALUES ");
        for (int r = 0; r < rows.Length; r++)
        {
            context.Write(r == 0 ? "(" : ", (");
            ImmutableArray<ParameterNode> row = rows[r];
            for (int i = 0; i < row.Length; i++)
            {
                if (i > 0)
                {
                    context.Write(", ");
                }

                row[i].Render(context);
            }

            context.Write(')');
        }
    }
}
